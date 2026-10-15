using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public sealed record DueScheduledDate(DateTime LocalDate, DateTime DueAtUtc);

    public static class ScheduledOccurrenceCalendar
    {
        public const string DefaultTimeZone = "America/Costa_Rica";
        public const int ScanWindowDays = 31;

        public static TimeZoneInfo ResolveTimeZone(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 128 ||
                (id != "UTC" && !TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out _)))
                throw new ServiceException("A supported IANA timezone is required.", HttpStatusCode.BadRequest);
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException)
            { throw new ServiceException("The timezone is not supported on this server.", HttpStatusCode.BadRequest); }
        }

        // Half-open UTC windows let the persisted cursor advance without dropping a boundary midnight.
        public static IReadOnlyList<DueScheduledDate> FindDueDates(BaseScheduledTaskVm definition,
            string timeZoneId, DateTime fromUtc, DateTime untilExclusiveUtc)
        {
            ArgumentNullException.ThrowIfNull(definition);
            ScheduledExecutionRules.ValidateSchedule(definition.FrequencyType, definition.Days);
            if (fromUtc.Kind != DateTimeKind.Utc || untilExclusiveUtc.Kind != DateTimeKind.Utc ||
                untilExclusiveUtc < fromUtc || untilExclusiveUtc - fromUtc > TimeSpan.FromDays(ScanWindowDays))
                throw new ArgumentException("A bounded UTC scan window is required.");
            var zone = ResolveTimeZone(timeZoneId);
            var result = new List<DueScheduledDate>();
            if (definition.FrequencyType == ScheduledTaskFrequencyType.Manual || fromUtc == untilExclusiveUtc)
                return result;
            // Local dates can move backward across midnight during an offset change.
            // UTC dates with a one-day margin cover every supported timezone offset.
            var first = fromUtc.Date == DateTime.MinValue.Date ? fromUtc.Date : fromUtc.Date.AddDays(-1);
            var last = untilExclusiveUtc.Date == DateTime.MaxValue.Date
                ? untilExclusiveUtc.Date : untilExclusiveUtc.Date.AddDays(1);
            var days = definition.Days.ToHashSet();
            for (var day = first; day <= last; day = day.AddDays(1))
            {
                if (!days.Contains(definition.FrequencyType == ScheduledTaskFrequencyType.Monthly
                    ? day.Day : (int)day.DayOfWeek)) continue;
                var due = MidnightUtc(day, zone);
                if (due.HasValue && due >= fromUtc && due < untilExclusiveUtc)
                    result.Add(new DueScheduledDate(DateTime.SpecifyKind(day, DateTimeKind.Unspecified), due.Value));
            }
            return result;
        }

        private static DateTime? MidnightUtc(DateTime date, TimeZoneInfo zone)
        {
            var local = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
            if (zone.IsInvalidTime(local))
            {
                var end = local.AddDays(1);
                var invalid = local;
                while (local < end && zone.IsInvalidTime(local))
                {
                    invalid = local;
                    local = local.AddMinutes(1);
                }
                if (local >= end) return null; // A completely skipped civil date has no occurrence.
                // Locate the first valid instant rather than assuming a whole-minute transition.
                var low = invalid.Ticks; var high = local.Ticks;
                while (high - low > 1)
                {
                    var middle = low + (high - low) / 2;
                    if (zone.IsInvalidTime(new DateTime(middle, DateTimeKind.Unspecified))) low = middle;
                    else high = middle;
                }
                local = new DateTime(high, DateTimeKind.Unspecified);
            }
            if (zone.IsAmbiguousTime(local))
                return new DateTimeOffset(local, zone.GetAmbiguousTimeOffsets(local).Max()).UtcDateTime;
            return TimeZoneInfo.ConvertTimeToUtc(local, zone);
        }
    }
}
