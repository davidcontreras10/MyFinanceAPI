using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public enum ScheduledOccurrenceStatus { Pending = 1, Completed = 2, Canceled = 3 }
    public enum ScheduledManualAction { RunTodayOccurrence = 1, ExtraExecution = 2 }
    public sealed record ScheduledOccurrenceSummary(Guid Id, DateTime ScheduledLocalDate, ScheduledOccurrenceStatus Status);
    public sealed record ScheduledTaskExecutionContext(Guid TaskId, string TimeZoneId, DateTime LocalDate,
        bool ScheduleActivated, bool DiscoveryIncomplete, bool ExecutionEnabled, DateTime? RetiredAtUtc,
        ScheduledOccurrenceSummary Today, ScheduledOccurrenceSummary EarliestPending,
        ScheduledManualAction? DefaultAction, bool WarnExtraExecution, int CanceledOccurrenceCount);

    public sealed record ScheduledTaskHistoryEntry
    {
        public Guid Id { get; init; }
        public Guid? ExecutionId { get; init; }
        public Guid? OccurrenceId { get; init; }
        public Guid? ManualRequestId { get; init; }
        public Guid? ActorId { get; init; }
        public DateTime PostingDate { get; init; }
        public DateTime EventAtUtc { get; init; }
        public ScheduledOccurrenceStatus Status { get; init; }
    }
    public sealed record ScheduledTaskHistoryPage(IReadOnlyList<ScheduledTaskHistoryEntry> Items, int Skip, int Limit, bool HasMore);

    public sealed record ScheduledTaskAccessScope
    {
        public Guid OwnerId { get; }
        public Guid TaskId { get; }
        public ResourceActionNames Action { get; }
        public ScheduledTaskAccessScope(Guid ownerId, Guid taskId, ResourceActionNames action)
        {
            if (ownerId == Guid.Empty || taskId == Guid.Empty || action is not (ResourceActionNames.View or ResourceActionNames.Delete))
                throw new ArgumentException("An owner, task and supported access action are required.");
            OwnerId = ownerId; TaskId = taskId; Action = action;
        }
    }
    public sealed record ScheduledTaskAccessAuthorizationResult(ScheduledTaskAccessScope Scope)
    {
        public bool IsAuthorized => Scope != null;
    }

    public sealed record ScheduledTaskExecutionScope
    {
        public Guid ActorId { get; }
        public Guid OwnerId { get; }
        public Guid TaskId { get; }
        public bool AdministratorDispatch { get; }
        public ScheduledTaskExecutionScope(Guid actorId, Guid ownerId, Guid taskId, bool administratorDispatch = false)
        {
            if (actorId == Guid.Empty || ownerId == Guid.Empty || taskId == Guid.Empty || (actorId != ownerId && !administratorDispatch))
                throw new ArgumentException("A valid execution scope is required.");
            ActorId = actorId; OwnerId = ownerId; TaskId = taskId; AdministratorDispatch = administratorDispatch;
        }
    }
    public sealed record ScheduledTaskExecutionAuthorizationResult(ScheduledTaskExecutionScope Scope)
    {
        public bool IsAuthorized => Scope != null;
    }
}
