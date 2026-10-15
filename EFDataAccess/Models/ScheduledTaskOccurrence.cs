using System;

namespace EFDataAccess.Models
{
    public sealed class ScheduledTaskOccurrence
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime ScheduledLocalDate { get; set; }
        public DateTime PostingDate { get; set; }
        public DateTime DueAtUtc { get; set; }
        public string TimeZoneId { get; set; }
        public string DefinitionJson { get; set; }
    }
}
