using System;

namespace EFDataAccess.Models
{
    public sealed class ScheduledTaskSchedule
    {
        public Guid TaskId { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime ActivatedAtUtc { get; set; }
        public DateTime NextScanUtc { get; set; }
    }
}
