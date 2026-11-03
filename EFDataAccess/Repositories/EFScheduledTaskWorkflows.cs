using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace EFDataAccess.Repositories
{
    public sealed partial class EFScheduledScheduleRepository
    {
        private const int MinimumHistoryOffset = 0;
        private const int MaximumHistoryOffset = 10000;
        private const int MinimumHistoryPageSize = 1;
        private const int MaximumHistoryPageSize = 100;

        private static void RequireAccess(ScheduledTaskAccessScope scope, ResourceActionNames action)
        {
            ArgumentNullException.ThrowIfNull(scope);
            if (scope.Action != action) throw new ServiceException("The approved scope does not permit this operation.", HttpStatusCode.Forbidden);
        }

        public async Task RetireAsync(ScheduledTaskAccessScope scope, CancellationToken cancellationToken = default)
        {
            RequireAccess(scope, ResourceActionNames.Delete);
            await InOwnerTransactionAsync(scope.OwnerId, scope.TaskId, async () =>
            {
                var task = await context.AutomaticTask.SingleOrDefaultAsync(x =>
                    x.AutomaticTaskId == scope.TaskId && x.UserId == scope.OwnerId, cancellationToken) ??
                    throw new ServiceException("Task not found within the approved scope.", HttpStatusCode.NotFound);
                task.RetiredAtUtc ??= clock.GetUtcNow().UtcDateTime;
                return true;
            }, cancellationToken, requireHistory: false);
        }

        public Task<TaskExecutedResult> ExecuteLegacyAsync(ScheduledTaskExecutionScope scope,
            Func<Task<TaskExecutedResult>> execute, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(scope);
            ArgumentNullException.ThrowIfNull(execute);
            if (options.HistoryEnabled)
                throw new ServiceException("Use execution-v2 with a stable occurrence or manual request identity.", HttpStatusCode.Conflict);
            return InOwnerTransactionAsync(scope.OwnerId, scope.TaskId, async () =>
            {
                var task = await LoadTask(scope.TaskId, cancellationToken);
                if (task.UserId != scope.OwnerId || (scope.ActorId != task.UserId &&
                    (!scope.AdministratorDispatch || !await IsAdministrator(scope.ActorId, cancellationToken))))
                    throw new ServiceException("Task access denied.", HttpStatusCode.Forbidden);
                if (task.RetiredAtUtc.HasValue) throw new ServiceException("The task is retired.", HttpStatusCode.Conflict);
                await CheckedDefinition(task, cancellationToken);
                var result = await execute();
                if (result.Status != ExecutedTaskStatus.Succeeded)
                    throw new ServiceException("Task execution failed without committing financial changes.", HttpStatusCode.Conflict);
                return result;
            }, cancellationToken, requireHistory: false);
        }

        public async Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(ScheduledTaskAccessScope scope,
            CancellationToken cancellationToken = default)
        {
            RequireHistory();
            RequireAccess(scope, ResourceActionNames.View);
            var task = await ScopedTask(scope, cancellationToken);
            var state = await context.Set<ScheduledTaskSchedule>().AsNoTracking().SingleOrDefaultAsync(x =>
                x.TaskId == scope.TaskId && x.OwnerId == scope.OwnerId, cancellationToken);
            var incomplete = false;
            if (state != null && !task.RetiredAtUtc.HasValue)
                incomplete = (await DiscoverAsync(scope.TaskId, scope.OwnerId, cancellationToken)).HasMore;
            task = await ScopedTask(scope, cancellationToken);
            var zone = await context.AppUser.AsNoTracking().Where(x => x.UserId == scope.OwnerId)
                .Select(x => x.SchedulingTimeZoneId).SingleAsync(cancellationToken);
            var now = clock.GetUtcNow();
            var today = TimeZoneInfo.ConvertTime(now, MyFinanceBackend.Services.ScheduledOccurrenceCalendar.ResolveTimeZone(zone)).Date;
            var completions = context.Set<ScheduledTaskCompletion>().AsNoTracking().Where(x => x.TaskId == scope.TaskId &&
                context.AutomaticTask.Any(parent => parent.AutomaticTaskId == x.TaskId && parent.UserId == scope.OwnerId));
            var occurrences = context.Set<ScheduledTaskOccurrence>().AsNoTracking().Where(x =>
                x.TaskId == scope.TaskId && x.OwnerId == scope.OwnerId &&
                context.AutomaticTask.Any(parent => parent.AutomaticTaskId == x.TaskId && parent.UserId == scope.OwnerId));
            var todays = await occurrences.SingleOrDefaultAsync(x => x.ScheduledLocalDate == today && x.DueAtUtc <= now.UtcDateTime, cancellationToken);
            ScheduledOccurrenceSummary todaySummary = null;
            if (todays != null)
            {
                var done = await completions.AnyAsync(x => x.OccurrenceId == todays.Id, cancellationToken);
                todaySummary = new(todays.Id, todays.ScheduledLocalDate,
                    done ? ScheduledOccurrenceStatus.Completed : task.RetiredAtUtc.HasValue ? ScheduledOccurrenceStatus.Canceled : ScheduledOccurrenceStatus.Pending);
            }
            var pending = task.RetiredAtUtc.HasValue ? null : await occurrences
                .Where(x => x.DueAtUtc <= now.UtcDateTime && !completions.Any(done => done.OccurrenceId == x.Id))
                .OrderBy(x => x.ScheduledLocalDate).FirstOrDefaultAsync(cancellationToken);
            ScheduledManualAction? action = task.RetiredAtUtc.HasValue || incomplete ? null :
                todaySummary?.Status == ScheduledOccurrenceStatus.Pending
                    ? pending?.Id == todaySummary.Id ? ScheduledManualAction.RunTodayOccurrence : null
                    : ScheduledManualAction.ExtraExecution;
            var canceled = task.RetiredAtUtc.HasValue
                ? await occurrences.CountAsync(x => !completions.Any(done => done.OccurrenceId == x.Id), cancellationToken) : 0;
            return new(scope.TaskId, zone, today, state != null, incomplete, options.Enabled,
                task.RetiredAtUtc.HasValue ? Utc(task.RetiredAtUtc.Value) : null, todaySummary,
                pending == null ? null : new(pending.Id, pending.ScheduledLocalDate, ScheduledOccurrenceStatus.Pending), action,
                todaySummary != null || pending != null || incomplete, canceled);
        }

        public async Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(ScheduledTaskAccessScope scope, int skip, int limit,
            CancellationToken cancellationToken = default)
        {
            RequireHistory();
            RequireAccess(scope, ResourceActionNames.View);
            if (skip < MinimumHistoryOffset || skip > MaximumHistoryOffset || limit < MinimumHistoryPageSize || limit > MaximumHistoryPageSize)
                throw new ServiceException($"History requires skip {MinimumHistoryOffset}-{MaximumHistoryOffset} and limit {MinimumHistoryPageSize}-{MaximumHistoryPageSize}.", HttpStatusCode.BadRequest);
            var task = await ScopedTask(scope, cancellationToken);
            var completions = context.Set<ScheduledTaskCompletion>().AsNoTracking().Where(x => x.TaskId == scope.TaskId &&
                context.AutomaticTask.Any(parent => parent.AutomaticTaskId == x.TaskId && parent.UserId == scope.OwnerId));
            var completed = completions.Select(x => new ScheduledTaskHistoryEntry
            {
                Id = x.Id, ExecutionId = x.Id, OccurrenceId = x.OccurrenceId, ManualRequestId = x.ManualRequestId,
                ActorId = x.ActorId, PostingDate = x.PostingDate, EventAtUtc = x.CompletedAtUtc, Status = ScheduledOccurrenceStatus.Completed
            });
            var retiredAt = task.RetiredAtUtc ?? DateTime.MinValue;
            var canceled = context.Set<ScheduledTaskOccurrence>().AsNoTracking().Where(x =>
                task.RetiredAtUtc.HasValue && x.TaskId == scope.TaskId && x.OwnerId == scope.OwnerId &&
                context.AutomaticTask.Any(parent => parent.AutomaticTaskId == x.TaskId && parent.UserId == scope.OwnerId) &&
                !completions.Any(done => done.OccurrenceId == x.Id)).Select(x => new ScheduledTaskHistoryEntry
                {
                    Id = x.Id, ExecutionId = null, OccurrenceId = x.Id, ManualRequestId = null, ActorId = null,
                    PostingDate = x.PostingDate, EventAtUtc = retiredAt, Status = ScheduledOccurrenceStatus.Canceled
                });
            var items = await completed.Concat(canceled).OrderByDescending(x => x.EventAtUtc).ThenByDescending(x => x.Id)
                .Skip(skip).Take(limit + 1).ToListAsync(cancellationToken);
            return new(items.Take(limit).Select(x => x with { EventAtUtc = Utc(x.EventAtUtc),
                PostingDate = DateTime.SpecifyKind(x.PostingDate, DateTimeKind.Unspecified) }).ToArray(), skip, limit, items.Count > limit);
        }

        private async Task<AutomaticTask> ScopedTask(ScheduledTaskAccessScope scope, CancellationToken cancellationToken) =>
            await context.AutomaticTask.AsNoTracking().SingleOrDefaultAsync(x =>
                x.AutomaticTaskId == scope.TaskId && x.UserId == scope.OwnerId, cancellationToken) ??
                throw new ServiceException("Task not found within the approved scope.", HttpStatusCode.NotFound);
    }
}
