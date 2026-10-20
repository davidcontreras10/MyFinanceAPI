using System;

namespace EFDataAccess.Models
{
    public sealed class ScheduledTaskCompletion
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid ActorId { get; set; }
        public Guid? OccurrenceId { get; set; }
        public Guid? ManualRequestId { get; set; }
        public DateTime PostingDate { get; set; }
        public DateTime CompletedAtUtc { get; set; }
    }
}
