using System;
using System.Collections.Generic;
using System.Data;
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
    public sealed partial class EFScheduledScheduleRepository(MyFinanceContext context, TimeProvider clock,
        IAccountAccessRepository accountAccess, ScheduledExecutionOptions options) : IScheduledScheduleRepository, IScheduledUserPreferencesRepository
    {
        private const int MinimumBatchSize = 1;
        private const int MaximumBatchSize = 100;
        private const int LockTimeoutMilliseconds = 10000;
        private const int LockBusyErrorNumber = 51001;
        private const string AdministratorRoleName = "Admin";

        public async Task<ScheduledUserPreferences> GetPreferencesAsync(Guid ownerId, CancellationToken cancellationToken = default)
        {
            RequireHistory();
            var zone = await context.AppUser.AsNoTracking().Where(x => x.UserId == ownerId)
                .Select(x => x.SchedulingTimeZoneId).SingleOrDefaultAsync(cancellationToken);
            if (zone == null) throw new ServiceException("User not found.", HttpStatusCode.NotFound);
            return new ScheduledUserPreferences(zone);
        }

        public Task<ScheduledUserPreferences> UpdatePreferencesAsync(Guid ownerId, string timeZoneId,
            CancellationToken cancellationToken = default) => InOwnerTransactionAsync(ownerId, null, async () =>
        {
            ScheduledOccurrenceCalendar.ResolveTimeZone(timeZoneId);
            var user = await context.AppUser.SingleOrDefaultAsync(x => x.UserId == ownerId, cancellationToken) ??
                throw new ServiceException("User not found.", HttpStatusCode.NotFound);
            if (user.SchedulingTimeZoneId == timeZoneId) return new ScheduledUserPreferences(timeZoneId);
            var states = await context.Set<ScheduledTaskSchedule>().Where(x => x.OwnerId == ownerId &&
                    context.AutomaticTask.Any(task => task.AutomaticTaskId == x.TaskId && task.RetiredAtUtc == null))
                .OrderBy(x => x.TaskId).ToListAsync(cancellationToken);
            foreach (var state in states)
            {
                await LockAsync($"scheduled-definition:{state.TaskId:N}", cancellationToken);
                if (!await context.AutomaticTask.AnyAsync(x => x.AutomaticTaskId == state.TaskId && x.UserId == ownerId, cancellationToken))
                    throw new ServiceException("The schedule no longer belongs to this owner.", HttpStatusCode.Conflict);
                var current = await context.Set<ScheduledTaskRevision>().SingleAsync(
                    x => x.TaskId == state.TaskId && x.EffectiveUntilUtc == null, cancellationToken);
                await AppendRevisionAsync(state, current, current.DefinitionJson, timeZoneId, cancellationToken);
            }
            user.SchedulingTimeZoneId = timeZoneId;
            return new ScheduledUserPreferences(timeZoneId);
        }, cancellationToken);

        public Task<Guid> RegisterAsync(Guid ownerId, Func<Task<Guid>> createTask,
            CancellationToken cancellationToken = default) => InOwnerTransactionAsync(ownerId, null, async () =>
        {
            ArgumentNullException.ThrowIfNull(createTask);
            var user = await context.AppUser.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == ownerId, cancellationToken) ??
                throw new ServiceException("User not found.", HttpStatusCode.NotFound);
            var id = await createTask();
            var task = await LoadTask(id, cancellationToken);
            if (task.UserId != ownerId) throw new ServiceException("The new task must belong to the approved owner.", HttpStatusCode.Forbidden);
            if (await context.Set<ScheduledTaskSchedule>().AnyAsync(x => x.TaskId == id, cancellationToken))
                throw new ServiceException("The task already has a registered schedule.", HttpStatusCode.Conflict);
            await AddScheduleAsync(task, clock.GetUtcNow().UtcDateTime, user.SchedulingTimeZoneId, cancellationToken);
            return id;
        }, cancellationToken);

        public Task ActivateAsync(Guid taskId, Guid administratorId, DateTimeOffset activationUtc,
            string timeZoneId, CancellationToken cancellationToken = default) =>
            InTransactionAsync(taskId, async () =>
            {
                if (!await IsAdministrator(administratorId, cancellationToken))
                    throw new ServiceException("Administrator permission is required to activate schedules.", HttpStatusCode.Forbidden);
                ScheduledOccurrenceCalendar.ResolveTimeZone(timeZoneId);
                var task = await LoadTask(taskId, cancellationToken);
                if (task.RetiredAtUtc.HasValue) throw new ServiceException("A retired task cannot be activated.", HttpStatusCode.Conflict);
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
                await AddScheduleAsync(task, start, timeZoneId, cancellationToken);
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
            if (task.RetiredAtUtc.HasValue) return new ScheduledDiscoveryResult(0, from, false);
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
                if (task.RetiredAtUtc.HasValue) throw new ServiceException("A retired task cannot be edited.", HttpStatusCode.Conflict);
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
                await AppendRevisionAsync(state, current, json, timeZoneId ?? current.TimeZoneId, cancellationToken);
                return true;
            }, cancellationToken);

        public async Task<IReadOnlyList<PendingScheduledOccurrence>> GetPendingAsync(Guid actorId,
            int limit, CancellationToken cancellationToken = default)
        {
            RequireHistory();
            if (actorId == Guid.Empty || limit < MinimumBatchSize || limit > MaximumBatchSize)
                throw new ServiceException($"A valid actor and batch limit from {MinimumBatchSize} to {MaximumBatchSize} are required.", HttpStatusCode.BadRequest);
            var administrator = await IsAdministrator(actorId, cancellationToken);
            var now = clock.GetUtcNow().UtcDateTime;
            var occurrences = context.Set<ScheduledTaskOccurrence>().AsNoTracking();
            var completions = context.Set<ScheduledTaskCompletion>();
            var pending = await occurrences.Where(x => (administrator || x.OwnerId == actorId) && x.DueAtUtc <= now &&
                context.AutomaticTask.Any(task => task.AutomaticTaskId == x.TaskId && task.UserId == x.OwnerId && task.RetiredAtUtc == null) &&
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
            if (limit < MinimumBatchSize || limit > MaximumBatchSize)
                throw new ServiceException($"A batch limit from {MinimumBatchSize} to {MaximumBatchSize} is required.", HttpStatusCode.BadRequest);
            var now = clock.GetUtcNow().UtcDateTime;
            return await context.Set<ScheduledTaskSchedule>().AsNoTracking()
                .Where(x => x.NextScanUtc <= now && context.AutomaticTask.Any(task => task.AutomaticTaskId == x.TaskId && task.UserId == x.OwnerId && task.RetiredAtUtc == null))
                .OrderBy(x => x.NextScanUtc).ThenBy(x => x.TaskId)
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
            context.AppUser.AnyAsync(x => x.UserId == actor && x.UserRoles.Any(role => role.Name == AdministratorRoleName), cancellationToken);

        private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

        private void RequireHistory()
        {
            if (!options.HistoryEnabled)
                throw new ServiceException("Schedule history is not enabled.", HttpStatusCode.ServiceUnavailable);
        }

        private async Task AddScheduleAsync(AutomaticTask task, DateTime start, string zone, CancellationToken cancellationToken)
        {
            if (task.RetiredAtUtc.HasValue) throw new ServiceException("A retired task cannot be activated.", HttpStatusCode.Conflict);
            ScheduledOccurrenceCalendar.ResolveTimeZone(zone);
            var definition = await CheckedDefinition(task, cancellationToken);
            context.Add(new ScheduledTaskSchedule
                { TaskId = task.AutomaticTaskId, OwnerId = task.UserId, ActivatedAtUtc = start, NextScanUtc = start });
            context.Add(new ScheduledTaskRevision
            {
                Id = Guid.NewGuid(), TaskId = task.AutomaticTaskId, EffectiveFromUtc = start,
                TimeZoneId = zone, DefinitionJson = ScheduledTaskDefinitionCodec.Write(definition)
            });
        }

        private async Task AppendRevisionAsync(ScheduledTaskSchedule state, ScheduledTaskRevision current,
            string json, string zone, CancellationToken cancellationToken)
        {
            if (current.DefinitionJson == json && current.TimeZoneId == zone) return;
            // Due work, including midnight at this instant, keeps its original context.
            var cutover = new[] { clock.GetUtcNow().UtcDateTime.AddTicks(1),
                Utc(state.NextScanUtc), Utc(current.EffectiveFromUtc).AddTicks(1) }.Max();
            current.EffectiveUntilUtc = cutover;
            await context.SaveChangesAsync(cancellationToken);
            context.Add(new ScheduledTaskRevision
            {
                Id = Guid.NewGuid(), TaskId = state.TaskId, EffectiveFromUtc = cutover,
                TimeZoneId = zone, DefinitionJson = json
            });
        }

        private async Task<T> InTransactionAsync<T>(Guid taskId, Func<Task<T>> operation, CancellationToken cancellationToken)
        {
            RequireHistory();
            if (taskId == Guid.Empty) throw new ServiceException("A task ID is required.", HttpStatusCode.BadRequest);
            var owner = await context.AutomaticTask.AsNoTracking().Where(x => x.AutomaticTaskId == taskId)
                .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken) ??
                throw new ServiceException("Task not found.", HttpStatusCode.NotFound);
            return await InOwnerTransactionAsync(owner, taskId, async () =>
            {
                if (!await context.AutomaticTask.AnyAsync(x => x.AutomaticTaskId == taskId && x.UserId == owner, cancellationToken))
                    throw new ServiceException("Task ownership changed; retry the request.", HttpStatusCode.Conflict);
                return await operation();
            }, cancellationToken);
        }

        private async Task LockAsync(string key, CancellationToken cancellationToken) =>
            await context.Database.ExecuteSqlInterpolatedAsync($@"
                DECLARE @result int, @busyError int = {LockBusyErrorNumber};
                EXEC @result = sys.sp_getapplock @Resource={key}, @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout={LockTimeoutMilliseconds};
                IF @result < 0 THROW @busyError, 'Schedule is busy; retry.', 1;", cancellationToken);

        private async Task<T> InOwnerTransactionAsync<T>(Guid ownerId, Guid? taskId, Func<Task<T>> operation,
            CancellationToken cancellationToken, bool requireHistory = true)
        {
            if (requireHistory) RequireHistory();
            if (ownerId == Guid.Empty) throw new ServiceException("An owner is required.", HttpStatusCode.BadRequest);
            if (context.Database.CurrentTransaction != null || context.ChangeTracker.HasChanges())
                throw new InvalidOperationException("Schedule changes require a clean scoped unit of work.");
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                // Owner first, task second: preference changes and creation use the same ordering.
                await LockAsync($"scheduled-owner:{ownerId:N}", cancellationToken);
                if (taskId.HasValue) await LockAsync($"scheduled-definition:{taskId.Value:N}", cancellationToken);
                var result = await operation();
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception error)
            {
                try { await transaction.RollbackAsync(CancellationToken.None); } catch { }
                context.ChangeTracker.Clear();
                if (error is Microsoft.Data.SqlClient.SqlException { Number: LockBusyErrorNumber })
                    throw new ServiceException("The schedule is busy; retry.", HttpStatusCode.Conflict);
                throw;
            }
        }
    }
}
