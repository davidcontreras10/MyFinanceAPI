using MyFinanceModel;

namespace EFDataAccess.Models
{
    public partial class AppUser
    {
        public string SchedulingTimeZoneId { get; set; } = ScheduledUserPreferences.DefaultTimeZone;
    }
}
