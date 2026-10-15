using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;

namespace EFDataAccess.Repositories
{
    // The HTTP service is feature-gated; this repository requires a dedicated scoped context.
    public sealed class EFScheduledExecutionRepository(MyFinanceContext context, TimeProvider clock,
        IAutomaticTaskRepository definitions, ILogger<EFScheduledExecutionRepository> logger)
        : IScheduledExecutionRepository
    {
        private const int LockTimeoutMilliseconds = 10000;
        private const int LockBusyErrorNumber = 51001;
        private const string AdministratorRoleName = "Admin";

        public async Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request,
            Func<ScheduledExecutionWork, CancellationToken, Task> execute,
            CancellationToken cancellationToken = default)
        {
            ScheduledExecutionRules.Validate(request);
            ArgumentNullException.ThrowIfNull(execute);
            if (context.Database.CurrentTransaction != null)
                throw new InvalidOperationException("Scheduled execution must own its transaction.");
            if (context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Scheduled execution requires a clean scoped unit of work.");

            var owner = await context.AutomaticTask.AsNoTracking().Where(x => x.AutomaticTaskId == request.TaskId)
                .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken) ??
                throw new ServiceException("Task not found.", HttpStatusCode.NotFound);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var ownerKey = $"scheduled-owner:{owner:N}";
                await context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int, @busyError int = {LockBusyErrorNumber};
                    EXEC @result = sys.sp_getapplock @Resource={ownerKey}, @LockMode='Exclusive',
                         @LockOwner='Transaction', @LockTimeout={LockTimeoutMilliseconds};
                    IF @result < 0 THROW @busyError, 'Scheduled execution is busy; retry the same request.', 1;", cancellationToken);
                // Transaction-owned SQL locks serialize retries across processes, with a bounded wait.
                var key = request.OccurrenceId.HasValue
                    ? $"scheduled-occurrence:{request.OccurrenceId:N}"
                    : $"scheduled-manual:{request.ActorId:N}:{request.ManualRequestId:N}";
                await context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int, @busyError int = {LockBusyErrorNumber};
                    EXEC @result = sys.sp_getapplock @Resource={key}, @LockMode='Exclusive',
                         @LockOwner='Transaction', @LockTimeout={LockTimeoutMilliseconds};
                    IF @result < 0 THROW @busyError, 'Scheduled execution is busy; retry the same request.', 1;", cancellationToken);

                var task = await context.AutomaticTask.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.AutomaticTaskId == request.TaskId, cancellationToken);
                if (task == null) throw new ServiceException("Task not found.", HttpStatusCode.NotFound);
                if (task.UserId != owner || (request.ExpectedOwnerId.HasValue && task.UserId != request.ExpectedOwnerId))
                    throw new ServiceException("Task ownership changed; refresh authorization.", HttpStatusCode.Conflict);
                var isOwner = task.UserId == request.ActorId;
                var isAdministrator = !isOwner && request.OccurrenceId.HasValue &&
                    await context.AppUser.AnyAsync(x => x.UserId == request.ActorId &&
                        x.UserRoles.Any(role => role.Name == AdministratorRoleName), cancellationToken);
                if (!isOwner && !isAdministrator)
                    throw new ServiceException("Task access denied.", HttpStatusCode.Forbidden);

                ScheduledTaskOccurrence occurrence = null;
                if (request.OccurrenceId.HasValue)
                {
                    occurrence = await context.Set<ScheduledTaskOccurrence>().AsNoTracking().SingleOrDefaultAsync(
                        x => x.Id == request.OccurrenceId && x.TaskId == request.TaskId && x.OwnerId == task.UserId,
                        cancellationToken);
                    if (occurrence == null || occurrence.DueAtUtc > clock.GetUtcNow().UtcDateTime)
                        throw new ServiceException("A due occurrence is required.", HttpStatusCode.Conflict);
                }

                var existing = await context.Set<ScheduledTaskCompletion>().AsNoTracking().SingleOrDefaultAsync(
                    x => request.OccurrenceId.HasValue ? x.OccurrenceId == request.OccurrenceId :
                        x.ActorId == request.ActorId && x.ManualRequestId == request.ManualRequestId, cancellationToken);
                if (existing != null)
                {
                    if (existing.TaskId != request.TaskId ||
                        (!request.OccurrenceId.HasValue && existing.PostingDate != request.ManualPostingDate))
                        throw new ServiceException("The request identity was already used for different work.", HttpStatusCode.Conflict);
                    await transaction.CommitAsync(cancellationToken);
                    return new ScheduledExecutionResult(existing.Id, true);
                }

                if (task.RetiredAtUtc.HasValue)
                    throw new ServiceException("The task is retired and pending work is canceled.", HttpStatusCode.Conflict);

                if (occurrence != null && await context.Set<ScheduledTaskOccurrence>().AnyAsync(
                    x => x.TaskId == request.TaskId && x.ScheduledLocalDate < occurrence.ScheduledLocalDate &&
                         !context.Set<ScheduledTaskCompletion>().Any(done => done.OccurrenceId == x.Id), cancellationToken))
                    throw new ServiceException("An earlier occurrence must complete first.", HttpStatusCode.Conflict);

                // Snapshot selection for scheduled work is independent of later task edits.
                var definition = occurrence?.DefinitionJson;
                if (definition == null)
                {
                    var currentDefinitions = await definitions.GetScheduledByTaskIdAsync(task.AutomaticTaskId.ToString());
                    definition = ScheduledTaskDefinitionCodec.Write(currentDefinitions.Single());
                }
                await execute(new ScheduledExecutionWork(task.AutomaticTaskId, task.UserId, request.ActorId,
                    occurrence?.PostingDate ?? request.ManualPostingDate.Value, definition, occurrence?.Id), cancellationToken);

                var completion = new ScheduledTaskCompletion
                {
                    Id = Guid.NewGuid(), TaskId = request.TaskId, ActorId = request.ActorId,
                    OccurrenceId = request.OccurrenceId, ManualRequestId = request.ManualRequestId,
                    PostingDate = occurrence?.PostingDate ?? request.ManualPostingDate.Value,
                    CompletedAtUtc = clock.GetUtcNow().UtcDateTime
                };
                await context.AddAsync(completion, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new ScheduledExecutionResult(completion.Id, false);
            }
            catch (Exception error)
            {
                // A fresh retry resolves an ambiguous commit through the durable completion key.
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { /* Preserve the original execution/commit failure. */ }
                context.ChangeTracker.Clear();
                // Diagnostics survive rollback and must not mask an ambiguous commit.
                try { logger.LogWarning("Scheduled execution did not return a committed result. Task {TaskId}; occurrence {OccurrenceId}; failure {FailureType}.",
                    request.TaskId, request.OccurrenceId, error.GetType().Name); }
                catch { /* Logging is best effort; a caller must still receive the original failure. */ }
                if (error is Microsoft.Data.SqlClient.SqlException { Number: LockBusyErrorNumber })
                    throw new ServiceException("Execution is busy; retry with the same identity.", HttpStatusCode.Conflict);
                throw;
            }
        }
    }
}
