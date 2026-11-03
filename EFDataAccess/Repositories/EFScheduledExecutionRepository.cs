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
    public sealed class EFScheduledExecutionRepository(MyFinanceContext context,
        DbContextOptions<MyFinanceContext> contextOptions, TimeProvider clock,
        IAutomaticTaskRepository definitions, ILogger<EFScheduledExecutionRepository> logger)
        : IScheduledExecutionRepository
    {
        private const int MinimumAttemptHistoryOffset = 0;
        private const int MaximumAttemptHistoryOffset = 10000;
        private const int MinimumAttemptHistoryPageSize = 1;
        private const int MaximumAttemptHistoryPageSize = 100;
        private const int LockTimeoutMilliseconds = 10000;
        private const int LockBusyErrorNumber = 51001;
        private const string AdministratorRoleName = "Admin";
        private const string AttemptStagePrepare = "PrepareExecution";
        private const string AttemptStageFinancialWork = "FinancialWork";
        private const string AttemptStageCompletion = "PersistCompletion";
        private const string AttemptStageCommit = "Commit";
        private const string AttemptStageRecovery = "Recovery";
        private const string InterruptedFailureType = "ProcessInterrupted";
        private const string InterruptedFailureMessage = "The prior process ended before recording an outcome.";
        private const string GenericFailureMessage = "Execution failed. See failure type and stage for diagnostics.";
        private const string CanceledFailureMessage = "Execution was canceled before completion.";
        private const string DatabaseFailureMessage = "A database operation failed during execution.";
        private const int MaximumFailureTypeLength = 200;
        private const int MaximumFailureMessageLength = 1000;

        public async Task<ScheduledExecutionAttemptHistoryPage> GetAttemptHistoryAsync(ScheduledTaskAccessScope scope,
            int skip, int limit, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scope);
            if (scope.Action != ResourceActionNames.View)
                throw new ServiceException("The approved scope does not permit viewing execution history.", HttpStatusCode.Forbidden);
            if (skip < MinimumAttemptHistoryOffset || skip > MaximumAttemptHistoryOffset ||
                limit < MinimumAttemptHistoryPageSize || limit > MaximumAttemptHistoryPageSize)
                throw new ServiceException($"Attempt history requires skip {MinimumAttemptHistoryOffset}-{MaximumAttemptHistoryOffset} and limit {MinimumAttemptHistoryPageSize}-{MaximumAttemptHistoryPageSize}.", HttpStatusCode.BadRequest);

            var taskExists = await context.AutomaticTask.AsNoTracking().AnyAsync(x =>
                x.AutomaticTaskId == scope.TaskId && x.UserId == scope.OwnerId, cancellationToken);
            if (!taskExists)
                throw new ServiceException("Task not found within the approved scope.", HttpStatusCode.NotFound);

            var attempts = await context.Set<ScheduledTaskExecutionAttempt>().AsNoTracking()
                .Where(x => x.TaskId == scope.TaskId && x.OwnerId == scope.OwnerId)
                .OrderByDescending(x => x.StartedAtUtc).ThenByDescending(x => x.Id)
                .Skip(skip).Take(limit + 1).ToListAsync(cancellationToken);
            var items = attempts.Take(limit).Select(x => new ScheduledExecutionAttemptHistoryEntry(
                x.Id, x.ActorId, x.OccurrenceId, x.ManualRequestId, x.AttemptNumber,
                DateTime.SpecifyKind(x.PostingDate, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(x.StartedAtUtc, DateTimeKind.Utc),
                x.EndedAtUtc.HasValue ? DateTime.SpecifyKind(x.EndedAtUtc.Value, DateTimeKind.Utc) : null,
                (ScheduledExecutionAttemptStatus)(int)x.Status, x.FailureStage, x.FailureType, x.FailureMessage)).ToArray();
            return new ScheduledExecutionAttemptHistoryPage(items, skip, limit, attempts.Count > limit);
        }

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
            Guid? attemptId = null;
            var failureStage = AttemptStagePrepare;
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
                    await TryFinalizeUnfinishedAttemptsAsync(request, existing.CompletedAtUtc);
                    return new ScheduledExecutionResult(existing.Id, true);
                }

                if (task.RetiredAtUtc.HasValue)
                    throw new ServiceException("The task is retired and pending work is canceled.", HttpStatusCode.Conflict);

                if (occurrence != null && await context.Set<ScheduledTaskOccurrence>().AnyAsync(
                    x => x.TaskId == request.TaskId && x.ScheduledLocalDate < occurrence.ScheduledLocalDate &&
                         !context.Set<ScheduledTaskCompletion>().Any(done => done.OccurrenceId == x.Id), cancellationToken))
                    throw new ServiceException("An earlier occurrence must complete first.", HttpStatusCode.Conflict);

                var postingDate = occurrence?.PostingDate ?? request.ManualPostingDate.Value;
                attemptId = await StartAttemptAsync(request, task.UserId, postingDate, cancellationToken);
                failureStage = AttemptStagePrepare;

                // Snapshot selection for scheduled work is independent of later task edits.
                var definition = occurrence?.DefinitionJson;
                if (definition == null)
                {
                    var currentDefinitions = await definitions.GetScheduledByTaskIdAsync(task.AutomaticTaskId.ToString());
                    definition = ScheduledTaskDefinitionCodec.Write(currentDefinitions.Single());
                }
                failureStage = AttemptStageFinancialWork;
                await execute(new ScheduledExecutionWork(task.AutomaticTaskId, task.UserId, request.ActorId,
                    postingDate, definition, occurrence?.Id), cancellationToken);

                failureStage = AttemptStageCompletion;
                var completion = new ScheduledTaskCompletion
                {
                    Id = Guid.NewGuid(), TaskId = request.TaskId, ActorId = request.ActorId,
                    OccurrenceId = request.OccurrenceId, ManualRequestId = request.ManualRequestId,
                    PostingDate = occurrence?.PostingDate ?? request.ManualPostingDate.Value,
                    CompletedAtUtc = clock.GetUtcNow().UtcDateTime
                };
                var attempt = await context.Set<ScheduledTaskExecutionAttempt>().SingleAsync(
                    x => x.Id == attemptId.Value, cancellationToken);
                attempt.Status = ScheduledTaskAttemptStatus.Succeeded;
                attempt.EndedAtUtc = completion.CompletedAtUtc;
                await context.AddAsync(completion, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                failureStage = AttemptStageCommit;
                await transaction.CommitAsync(cancellationToken);
                return new ScheduledExecutionResult(completion.Id, false);
            }
            catch (Exception error)
            {
                // A fresh retry resolves an ambiguous commit through the durable completion key.
                try { await transaction.RollbackAsync(CancellationToken.None); }
                catch { /* Preserve the original execution/commit failure. */ }
                context.ChangeTracker.Clear();
                if (attemptId.HasValue)
                {
                    var resolution = await TryFindCommittedCompletionAsync(request);
                    if (resolution.Resolved && resolution.Completion != null)
                    {
                        await TryFinalizeAttemptAsync(attemptId.Value, ScheduledTaskAttemptStatus.Succeeded,
                            null, null, null, resolution.Completion.CompletedAtUtc);
                        return new ScheduledExecutionResult(resolution.Completion.Id, true);
                    }
                    if (resolution.Resolved)
                    {
                        await TryFinalizeAttemptAsync(attemptId.Value, ScheduledTaskAttemptStatus.Failed,
                            failureStage, error, null, clock.GetUtcNow().UtcDateTime);
                    }
                }
                // Diagnostics survive rollback and must not mask an ambiguous commit.
                try { logger.LogWarning("Scheduled execution did not return a committed result. Task {TaskId}; occurrence {OccurrenceId}; failure {FailureType}.",
                    request.TaskId, request.OccurrenceId, error.GetType().Name); }
                catch { /* Logging is best effort; a caller must still receive the original failure. */ }
                if (error is Microsoft.Data.SqlClient.SqlException { Number: LockBusyErrorNumber })
                    throw new ServiceException("Execution is busy; retry with the same identity.", HttpStatusCode.Conflict);
                throw;
            }
        }

        private async Task<Guid> StartAttemptAsync(ScheduledExecutionRequest request, Guid ownerId,
            DateTime postingDate, CancellationToken cancellationToken)
        {
            await using var attemptContext = new MyFinanceContext(contextOptions);
            var matchingAttempts = GetIdentityAttempts(attemptContext, request);
            var now = clock.GetUtcNow().UtcDateTime;
            var unfinished = await matchingAttempts.Where(x => x.Status == ScheduledTaskAttemptStatus.InProgress)
                .ToListAsync(cancellationToken);
            foreach (var prior in unfinished)
            {
                prior.Status = ScheduledTaskAttemptStatus.Interrupted;
                prior.EndedAtUtc = now;
                prior.FailureStage = AttemptStageRecovery;
                prior.FailureType = InterruptedFailureType;
                prior.FailureMessage = InterruptedFailureMessage;
            }

            var attemptNumber = (await GetIdentityAttempts(attemptContext, request)
                .Select(x => (int?)x.AttemptNumber).MaxAsync(cancellationToken) ?? 0) + 1;
            var attempt = new ScheduledTaskExecutionAttempt
            {
                Id = Guid.NewGuid(),
                TaskId = request.TaskId,
                OwnerId = ownerId,
                ActorId = request.ActorId,
                OccurrenceId = request.OccurrenceId,
                ManualRequestId = request.ManualRequestId,
                AttemptNumber = attemptNumber,
                PostingDate = postingDate,
                StartedAtUtc = now,
                Status = ScheduledTaskAttemptStatus.InProgress
            };
            await attemptContext.AddAsync(attempt, cancellationToken);
            await attemptContext.SaveChangesAsync(cancellationToken);
            return attempt.Id;
        }

        private async Task TryFinalizeAttemptAsync(Guid attemptId, ScheduledTaskAttemptStatus status,
            string failureStage, Exception failure, DateTime? endedAtUtc, DateTime? knownEndedAtUtc)
        {
            try
            {
                await using var attemptContext = new MyFinanceContext(contextOptions);
                var attempt = await attemptContext.Set<ScheduledTaskExecutionAttempt>().SingleOrDefaultAsync(
                    x => x.Id == attemptId);
                if (attempt == null || attempt.Status != ScheduledTaskAttemptStatus.InProgress) return;

                attempt.Status = status;
                attempt.EndedAtUtc = knownEndedAtUtc ?? endedAtUtc;
                if (failure != null)
                {
                    attempt.FailureStage = failureStage;
                    attempt.FailureType = Truncate(failure.GetType().Name, MaximumFailureTypeLength);
                    attempt.FailureMessage = GetSafeFailureMessage(failure);
                }
                await attemptContext.SaveChangesAsync();
            }
            catch (Exception diagnosticError)
            {
                try
                {
                    logger.LogWarning("Could not finalize scheduled attempt {AttemptId}; diagnostic write failed with {FailureType}.",
                        attemptId, diagnosticError.GetType().Name);
                }
                catch { /* Diagnostic persistence must not change the financial outcome. */ }
            }
        }

        private async Task TryFinalizeUnfinishedAttemptsAsync(ScheduledExecutionRequest request, DateTime completedAtUtc)
        {
            try
            {
                await using var attemptContext = new MyFinanceContext(contextOptions);
                var unfinished = await GetIdentityAttempts(attemptContext, request)
                    .Where(x => x.Status == ScheduledTaskAttemptStatus.InProgress).ToListAsync();
                foreach (var attempt in unfinished)
                {
                    attempt.Status = ScheduledTaskAttemptStatus.Succeeded;
                    attempt.EndedAtUtc = completedAtUtc;
                    attempt.FailureStage = null;
                    attempt.FailureType = null;
                    attempt.FailureMessage = null;
                }
                if (unfinished.Count > 0) await attemptContext.SaveChangesAsync();
            }
            catch (Exception diagnosticError)
            {
                try
                {
                    logger.LogWarning("Could not reconcile unfinished attempts for task {TaskId}; diagnostic write failed with {FailureType}.",
                        request.TaskId, diagnosticError.GetType().Name);
                }
                catch { /* The existing completion remains authoritative. */ }
            }
        }

        private async Task<(bool Resolved, ScheduledTaskCompletion Completion)> TryFindCommittedCompletionAsync(
            ScheduledExecutionRequest request)
        {
            try
            {
                await using var completionContext = new MyFinanceContext(contextOptions);
                var completion = await GetCompletions(completionContext, request).AsNoTracking().SingleOrDefaultAsync();
                return (true, completion);
            }
            catch (Exception diagnosticError)
            {
                try
                {
                    logger.LogWarning("Could not resolve the scheduled completion after an execution error for task {TaskId}; lookup failed with {FailureType}.",
                        request.TaskId, diagnosticError.GetType().Name);
                }
                catch { /* Preserve the original execution error. */ }
                return (false, null);
            }
        }

        private static IQueryable<ScheduledTaskExecutionAttempt> GetIdentityAttempts(MyFinanceContext attemptContext,
            ScheduledExecutionRequest request)
        {
            var query = attemptContext.Set<ScheduledTaskExecutionAttempt>().Where(x => x.TaskId == request.TaskId);
            return request.OccurrenceId.HasValue
                ? query.Where(x => x.OccurrenceId == request.OccurrenceId)
                : query.Where(x => x.OccurrenceId == null && x.ActorId == request.ActorId &&
                    x.ManualRequestId == request.ManualRequestId);
        }

        private static IQueryable<ScheduledTaskCompletion> GetCompletions(MyFinanceContext completionContext,
            ScheduledExecutionRequest request)
        {
            var query = completionContext.Set<ScheduledTaskCompletion>().Where(x => x.TaskId == request.TaskId);
            return request.OccurrenceId.HasValue
                ? query.Where(x => x.OccurrenceId == request.OccurrenceId)
                : query.Where(x => x.OccurrenceId == null && x.ActorId == request.ActorId &&
                    x.ManualRequestId == request.ManualRequestId);
        }

        private static string GetSafeFailureMessage(Exception failure)
        {
            var message = failure switch
            {
                ServiceException => failure.Message,
                Microsoft.Data.SqlClient.SqlException => DatabaseFailureMessage,
                OperationCanceledException => CanceledFailureMessage,
                _ => GenericFailureMessage
            };
            return Truncate(message, MaximumFailureMessageLength);
        }

        private static string Truncate(string value, int maximumLength) =>
            string.IsNullOrEmpty(value) || value.Length <= maximumLength ? value : value[..maximumLength];
    }
}
