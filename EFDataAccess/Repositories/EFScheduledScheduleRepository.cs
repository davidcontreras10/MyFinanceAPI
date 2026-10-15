using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace EFDataAccess.Repositories
{
    // Activation is explicit. Merely deploying these tables must never replay legacy tasks.
    public sealed class EFScheduledScheduleRepository(MyFinanceContext context, TimeProvider clock,
        IAccountAccessRepository accountAccess, ScheduledExecutionOptions options) : IScheduledScheduleRepository
    {
        public Task ActivateAsync(Guid taskId, Guid administratorId, DateTimeOffset activationUtc,
            string timeZoneId, CancellationToken cancellationToken = default) =>
            InTransactionAsync(taskId, async () =>
            {
                if (!await IsAdministrator(administratorId, cancellationToken))
                    throw new ServiceException("Administrator permission is required to activate schedules.", HttpStatusCode.Forbidden);
                ScheduledOccurrenceCalendar.ResolveTimeZone(timeZoneId);
                var task = await LoadTask(taskId, cancellationToken);
                var start = activationUtc.UtcDateTime;
                if (start > clock.GetUtcNow().UtcDateTime)
                    throw new ServiceException("Activation cannot be in the future.", HttpStatusCode.BadRequest);
                var state = await context.Set<ScheduledTaskSchedule>().FindAsync(new object[] { taskId }, cancellationToken);
                if (state != null)
                {
                    var first = await context.Set<ScheduledTaskRevision>().OrderBy(x => x.EffectiveFromUtc)
                        .FirstAsync(x => x.TaskId == taskId, cancellationToken);
                    if (state.ActivatedAtUtc != start || first.TimeZoneId != timeZoneId)
                        throw new ServiceException("The existing activation boundary cannot be reset.", HttpStatusCode.Conflict);
                    return true;
                }
                var definition = await CheckedDefinition(task, cancellationToken);
                context.Add(new ScheduledTaskSchedule
                    { TaskId = taskId, OwnerId = task.UserId, ActivatedAtUtc = start, NextScanUtc = start });
                context.Add(new ScheduledTaskRevision
                {
                    Id = Guid.NewGuid(), TaskId = taskId, EffectiveFromUtc = start,
                    TimeZoneId = timeZoneId, DefinitionJson = ScheduledTaskDefinitionCodec.Write(definition)
                });
                return true;
            }, cancellationToken);

        public Task<ScheduledDiscoveryResult> DiscoverAsync(Guid taskId, Guid actorId,
            CancellationToken cancellationToken = default) => InTransactionAsync(taskId, async () =>
        {
            var task = await LoadTask(taskId, cancellationToken);
            if (task.UserId != actorId && !await IsAdministrator(actorId, cancellationToken))
                throw new ServiceException("Task access denied.", HttpStatusCode.Forbidden);
            var state = await context.Set<ScheduledTaskSchedule>().FindAsync(new object[] { taskId }, cancellationToken);
            if (state == null || state.OwnerId != task.UserId)
                throw new ServiceException("The task has no activated schedule for its owner.", HttpStatusCode.Conflict);
            var now = clock.GetUtcNow().UtcDateTime;
            var from = Utc(state.NextScanUtc);
            if (from > now) return new ScheduledDiscoveryResult(0, from, false);
            var until = new[] { now.AddTicks(1), from.AddDays(ScheduledOccurrenceCalendar.ScanWindowDays) }.Min();
            var revisions = await context.Set<ScheduledTaskRevision>().AsNoTracking()
                .Where(x => x.TaskId == taskId && x.EffectiveFromUtc < until &&
                    (!x.EffectiveUntilUtc.HasValue || x.EffectiveUntilUtc > from))
                .OrderBy(x => x.EffectiveFromUtc).ToListAsync(cancellationToken);
            var candidates = new List<ScheduledTaskOccurrence>();
            foreach (var revision in revisions)
            {
                var windowFrom = from > revision.EffectiveFromUtc ? from : Utc(revision.EffectiveFromUtc);
                var windowUntil = revision.EffectiveUntilUtc.HasValue && revision.EffectiveUntilUtc < until
                    ? Utc(revision.EffectiveUntilUtc.Value) : until;
                var definition = ScheduledTaskDefinitionCodec.Read(revision.DefinitionJson);
                foreach (var date in ScheduledOccurrenceCalendar.FindDueDates(definition,
                    revision.TimeZoneId, windowFrom, windowUntil))
                    candidates.Add(new ScheduledTaskOccurrence
                    {
                        Id = Guid.NewGuid(), TaskId = taskId, OwnerId = state.OwnerId,
                        ScheduledLocalDate = date.LocalDate, PostingDate = date.LocalDate,
                        DueAtUtc = date.DueAtUtc, TimeZoneId = revision.TimeZoneId,
                        DefinitionJson = revision.DefinitionJson
                    });
            }
            var added = 0;
            if (candidates.Count > 0)
            {
                var earliest = candidates.Min(x => x.ScheduledLocalDate);
                var latest = candidates.Max(x => x.ScheduledLocalDate);
                var existing = (await context.Set<ScheduledTaskOccurrence>().AsNoTracking()
                    .Where(x => x.TaskId == taskId && x.ScheduledLocalDate >= earliest && x.ScheduledLocalDate <= latest)
                    .Select(x => x.ScheduledLocalDate).ToListAsync(cancellationToken)).ToHashSet();
                foreach (var occurrence in candidates.OrderBy(x => x.DueAtUtc))
                    if (existing.Add(occurrence.ScheduledLocalDate)) { context.Add(occurrence); added++; }
            }
            // The cursor and all newly discovered dates are saved in the same transaction.
            state.NextScanUtc = until;
            return new ScheduledDiscoveryResult(added, until, until <= now);
        }, cancellationToken);

        public Task ReviseAsync(Guid taskId, Guid ownerId, Func<Task> update,
            string timeZoneId = null, CancellationToken cancellationToken = default) =>
            InTransactionAsync(taskId, async () =>
            {
                ArgumentNullException.ThrowIfNull(update);
                if (timeZoneId != null) ScheduledOccurrenceCalendar.ResolveTimeZone(timeZoneId);
                var task = await LoadTask(taskId, cancellationToken);
                if (ownerId == Guid.Empty || task.UserId != ownerId)
                    throw new ServiceException("Only the owner can revise a task.", HttpStatusCode.Forbidden);
                await update();
                if (task.UserId != ownerId)
                    throw new ServiceException("The task owner cannot be changed.", HttpStatusCode.Conflict);
                var definition = await CheckedDefinition(task, cancellationToken);
                var state = await context.Set<ScheduledTaskSchedule>().FindAsync(new object[] { taskId }, cancellationToken);
                if (state == null) return true; // Editing alone is not an activation request.
                if (state.OwnerId != ownerId)
                    throw new ServiceException("The activated schedule belongs to a different owner.", HttpStatusCode.Conflict);
                var current = await context.Set<ScheduledTaskRevision>()
                    .SingleAsync(x => x.TaskId == taskId && x.EffectiveUntilUtc == null, cancellationToken);
                var json = ScheduledTaskDefinitionCodec.Write(definition);
                var zone = timeZoneId ?? current.TimeZoneId;
                if (current.DefinitionJson == json && current.TimeZoneId == zone) return true;
                // A midnight already due at the edit instant retains its old definition.
                var cutover = new[] { clock.GetUtcNow().UtcDateTime.AddTicks(1),
                    Utc(state.NextScanUtc), Utc(current.EffectiveFromUtc).AddTicks(1) }.Max();
                current.EffectiveUntilUtc = cutover;
                await context.SaveChangesAsync(cancellationToken); // Close the filtered unique index before inserting its successor.
                context.Add(new ScheduledTaskRevision
                {
                    Id = Guid.NewGuid(), TaskId = taskId, EffectiveFromUtc = cutover,
                    TimeZoneId = zone, DefinitionJson = json
                });
                return true;
            }, cancellationToken);

        public async Task<IReadOnlyList<PendingScheduledOccurrence>> GetPendingAsync(Guid actorId,
            int limit, CancellationToken cancellationToken = default)
        {
            RequireHistory();
            if (actorId == Guid.Empty || limit < 1 || limit > 100)
                throw new ServiceException("A valid actor and batch limit from 1 to 100 are required.", HttpStatusCode.BadRequest);
            var administrator = await IsAdministrator(actorId, cancellationToken);
            var now = clock.GetUtcNow().UtcDateTime;
            var occurrences = context.Set<ScheduledTaskOccurrence>().AsNoTracking();
            var completions = context.Set<ScheduledTaskCompletion>();
            var pending = await occurrences.Where(x => (administrator || x.OwnerId == actorId) && x.DueAtUtc <= now &&
                !completions.Any(done => done.OccurrenceId == x.Id) &&
                !occurrences.Any(older => older.TaskId == x.TaskId && older.ScheduledLocalDate < x.ScheduledLocalDate &&
                    !completions.Any(done => done.OccurrenceId == older.Id)))
                .OrderBy(x => x.DueAtUtc).ThenBy(x => x.TaskId).Take(limit)
                .Select(x => new PendingScheduledOccurrence(x.Id, x.TaskId, x.OwnerId, x.ScheduledLocalDate, x.DueAtUtc))
                .ToListAsync(cancellationToken);
            return pending.Select(x => x with { DueAtUtc = Utc(x.DueAtUtc),
                ScheduledLocalDate = DateTime.SpecifyKind(x.ScheduledLocalDate, DateTimeKind.Unspecified) }).ToList();
        }

        private async Task<AutomaticTask> LoadTask(Guid taskId, CancellationToken cancellationToken) =>
            await context.AutomaticTask.Include(x => x.SpInTrxDef).Include(x => x.TransferTrxDef)
                .SingleOrDefaultAsync(x => x.AutomaticTaskId == taskId, cancellationToken) ??
                throw new ServiceException("Task not found.", HttpStatusCode.NotFound);

        public async Task<IReadOnlyList<Guid>> GetSchedulesToScanAsync(Guid administratorId,
            int limit, CancellationToken cancellationToken = default)
        {
            RequireHistory();
            if (!await IsAdministrator(administratorId, cancellationToken))
                throw new ServiceException("Administrator permission is required for cross-user discovery.", HttpStatusCode.Forbidden);
            if (limit < 1 || limit > 100)
                throw new ServiceException("A batch limit from 1 to 100 is required.", HttpStatusCode.BadRequest);
            var now = clock.GetUtcNow().UtcDateTime;
            return await context.Set<ScheduledTaskSchedule>().AsNoTracking()
                .Where(x => x.NextScanUtc <= now).OrderBy(x => x.NextScanUtc).ThenBy(x => x.TaskId)
                .Take(limit).Select(x => x.TaskId).ToListAsync(cancellationToken);
        }

        private async Task<BaseScheduledTaskVm> CheckedDefinition(AutomaticTask task, CancellationToken cancellationToken)
        {
            var definition = ScheduledTaskSnapshots.Create(task);
            var accounts = definition is TransferScheduledTaskVm transfer
                ? new[] { definition.AccountId, transfer.ToAccountId } : new[] { definition.AccountId };
            if (accounts.Any(x => x <= 0) || definition.SpendTypeId <= 0 || definition.CurrencyId <= 0 ||
                !float.IsFinite(definition.Amount) || definition.Amount <= 0)
                throw new ServiceException("The task's financial definition is invalid.", HttpStatusCode.BadRequest);
            if (!await accountAccess.IsWithinOwnerScopeAsync(task.UserId,
                new AccountAccessTargets { AccountIds = accounts, SpendTypeIds = new[] { definition.SpendTypeId } }))
                throw new ServiceException("Task references are outside the owner's scope.", HttpStatusCode.Forbidden);
            cancellationToken.ThrowIfCancellationRequested();
            return definition;
        }

        private Task<bool> IsAdministrator(Guid actor, CancellationToken cancellationToken) =>
            context.AppUser.AnyAsync(x => x.UserId == actor && x.UserRoles.Any(role => role.Name == "Admin"), cancellationToken);

        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private void RequireHistory()
        {
            if (!options.HistoryEnabled)
                throw new ServiceException("Schedule history is not enabled.", HttpStatusCode.ServiceUnavailable);
        }

        private async Task<T> InTransactionAsync<T>(Guid taskId, Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            RequireHistory();
            if (taskId == Guid.Empty) throw new ServiceException("A task ID is required.", HttpStatusCode.BadRequest);
            if (context.Database.CurrentTransaction != null || context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Schedule changes require a clean scoped unit of work.");
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var key = $"scheduled-definition:{taskId:N}";
                await context.Database.ExecuteSqlInterpolatedAsync($@"
                    DECLARE @result int;
                    EXEC @result = sys.sp_getapplock @Resource={key}, @LockMode='Exclusive',
                        @LockOwner='Transaction', @LockTimeout=10000;
                    IF @result < 0 THROW 51001, 'Schedule is busy; retry.', 1;", cancellationToken);
                var result = await operation();
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception error)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); } catch { }
                context.ChangeTracker.Clear();
                if (error is Microsoft.Data.SqlClient.SqlException { Number: 51001 })
                    throw new ServiceException("The schedule is busy; retry.", HttpStatusCode.Conflict);
                throw;
            }
        }
    }
}
