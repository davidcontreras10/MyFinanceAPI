using System;

namespace EFDataAccess.Models
{
    public sealed class ScheduledTaskRevision
    {
        public Guid Id { get; set; }
        public Guid TaskId { get; set; }
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveUntilUtc { get; set; }
        public string TimeZoneId { get; set; }
        public string DefinitionJson { get; set; }
    }
}
