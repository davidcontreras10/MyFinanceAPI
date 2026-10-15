using System;

namespace MyFinanceModel
{
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
