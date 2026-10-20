using System.ComponentModel.DataAnnotations;

namespace MyFinanceModel
{
    public sealed record ScheduledUserPreferences(string TimeZoneId)
    {
        public const string DefaultTimeZone = "America/Costa_Rica";
    }

    public sealed class UpdateScheduledUserPreferences
    {
        private const int MaximumTimeZoneIdLength = 128;

        [Required, MaxLength(MaximumTimeZoneIdLength)]
        public string TimeZoneId { get; set; }
    }
}
