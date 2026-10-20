using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public enum ScheduledExecutionAttemptStatus
    {
        InProgress = 1,
        Succeeded = 2,
        Failed = 3,
        Interrupted = 4
    }

    public sealed record ScheduledExecutionAttemptHistoryEntry(
        Guid Id, Guid ActorId, Guid? OccurrenceId, Guid? ManualRequestId, int AttemptNumber,
        DateTime PostingDate, DateTime StartedAtUtc, DateTime? EndedAtUtc,
        ScheduledExecutionAttemptStatus Status, string FailureStage, string FailureType, string FailureMessage);

    public sealed record ScheduledExecutionAttemptHistoryPage(
        IReadOnlyList<ScheduledExecutionAttemptHistoryEntry> Items, int Skip, int Limit, bool HasMore);

    public sealed class ScheduledExecutionInput
    {
        public Guid? OccurrenceId { get; set; }
        public Guid? ManualRequestId { get; set; }
        public DateTime? PostingDate { get; set; }
    }

    // An occurrence is shared by automatic calls and "Run Today's Occurrence".
    public sealed record ScheduledExecutionRequest(
        Guid TaskId, Guid ActorId, Guid? OccurrenceId, Guid? ManualRequestId,
        DateTime? ManualPostingDate, Guid? ExpectedOwnerId = null);

    public sealed record ScheduledExecutionWork(
        Guid TaskId, Guid OwnerId, Guid ActorId, DateTime PostingDate,
        string DefinitionJson, Guid? OccurrenceId);

    public sealed record ScheduledExecutionResult(Guid ExecutionId, bool AlreadyCompleted);
}
