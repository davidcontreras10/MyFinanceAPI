using System;

namespace EFDataAccess.Models
{
    public enum ScheduledTaskAttemptStatus
    {
        InProgress = 1,
        Succeeded = 2,
        Failed = 3,
        Interrupted = 4
    }

    public sealed class ScheduledTaskExecutionAttempt
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid ActorId { get; set; }
        public Guid? OccurrenceId { get; set; }
        public Guid? ManualRequestId { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime PostingDate { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public ScheduledTaskAttemptStatus Status { get; set; }
        public string FailureStage { get; set; }
        public string FailureType { get; set; }
        public string FailureMessage { get; set; }
    }
}
