using System;
using System.Linq;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class ScheduledOccurrenceCalendarTest
    {
        private static BasicScheduledTaskVm Weekly(params int[] days) => new()
            { FrequencyType = ScheduledTaskFrequencyType.Weekly, Days = days };

        private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0) =>
            new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

        [Test]
        public void HalfOpenWindowsIncludeMidnightExactlyOnce()
        {
            var definition = Weekly(Enumerable.Range(0, 7).ToArray());
            var first = ScheduledOccurrenceCalendar.FindDueDates(definition, "America/Costa_Rica", Utc(2026, 1, 14, 6), Utc(2026, 1, 15, 6));
            var second = ScheduledOccurrenceCalendar.FindDueDates(definition, "America/Costa_Rica", Utc(2026, 1, 15, 6), Utc(2026, 1, 16, 6));
            Assert.That(first.Single().LocalDate, Is.EqualTo(new DateTime(2026, 1, 14)));
            Assert.That(second.Single().LocalDate, Is.EqualTo(new DateTime(2026, 1, 15)));
            Assert.That(first.Single().DueAtUtc, Is.EqualTo(Utc(2026, 1, 14, 6)));
            Assert.That(first.Single().LocalDate.Kind, Is.EqualTo(DateTimeKind.Unspecified));
        }

        [TestCase(2026, 3, 29, 0)]
        [TestCase(2026, 1, 31, 6)]
        public void WeeklyCanRunAfterThe28thIncludingSunday(int year, int month, int day, int weekday)
        {
            var start = Utc(year, month, day, 6);
            Assert.That(ScheduledOccurrenceCalendar.FindDueDates(Weekly(weekday), "America/Costa_Rica", start, start.AddDays(1)), Has.Count.EqualTo(1));
        }

        [Test]
        public void Monthly28thIsRecoveredUsingItsOriginalDate()
        {
            var definition = new BasicScheduledTaskVm { FrequencyType = ScheduledTaskFrequencyType.Monthly, Days = new[] { 28 } };
            var dates = ScheduledOccurrenceCalendar.FindDueDates(definition, "America/Costa_Rica", Utc(2026, 1, 27), Utc(2026, 1, 31));
            Assert.That(dates.Single().LocalDate, Is.EqualTo(new DateTime(2026, 1, 28)));
            definition.Days = new[] { 29 };
            Assert.Throws<ServiceException>(() => ScheduledOccurrenceCalendar.FindDueDates(definition, "UTC", Utc(2026, 1, 27), Utc(2026, 1, 31)));
        }

        [Test]
        public void QuarterHourZoneIsDueOnTheFirstLaterHourlyCheck()
        {
            var dates = ScheduledOccurrenceCalendar.FindDueDates(Weekly(Enumerable.Range(0, 7).ToArray()),
                "Asia/Kathmandu", Utc(2026, 1, 13, 18), Utc(2026, 1, 13, 19));
            Assert.That(dates.Single().LocalDate, Is.EqualTo(new DateTime(2026, 1, 14)));
            Assert.That(dates.Single().DueAtUtc, Is.EqualTo(Utc(2026, 1, 13, 18, 15)));
        }

        [TestCase(3, 8, 5)]
        [TestCase(11, 1, 4)]
        public void MidnightGapOrAmbiguityUsesTheFirstValidInstant(int month, int day, int expectedUtcHour)
        {
            var dates = ScheduledOccurrenceCalendar.FindDueDates(Weekly(0), "America/Havana",
                Utc(2026, month, day), Utc(2026, month, day).AddDays(1));
            Assert.That(dates.Single().DueAtUtc, Is.EqualTo(Utc(2026, month, day, expectedUtcHour)));
        }

        [Test]
        public void DstDoesNotTurnLocalDaysIntoFixed24HourIntervals()
        {
            var dates = ScheduledOccurrenceCalendar.FindDueDates(Weekly(Enumerable.Range(0, 7).ToArray()),
                "America/New_York", Utc(2026, 3, 7), Utc(2026, 3, 11));
            Assert.That(dates.Select(x => x.DueAtUtc.Hour), Is.EqualTo(new[] { 5, 5, 4, 4 }));
            Assert.That(dates[2].DueAtUtc - dates[1].DueAtUtc, Is.EqualTo(TimeSpan.FromHours(23)));
        }

        [Test]
        public void RepeatedMidnightDoesNotCreateASecondOccurrenceOnTheLaterOffset()
        {
            Assert.That(ScheduledOccurrenceCalendar.FindDueDates(Weekly(0), "America/Havana",
                Utc(2026, 11, 1, 5), Utc(2026, 11, 1, 6)), Is.Empty);
        }

        [Test]
        public void ManualOnlyNeverCreatesAnAutomaticOccurrence()
        {
            var definition = new BasicScheduledTaskVm { FrequencyType = ScheduledTaskFrequencyType.Manual };
            Assert.That(ScheduledOccurrenceCalendar.FindDueDates(definition, "UTC", Utc(2026, 1, 1), Utc(2026, 2, 1)), Is.Empty);
        }

        [Test]
        public void InvalidWindowOrTimezoneFailsInsteadOfGuessing()
        {
            var definition = Weekly(0);
            Assert.Throws<ArgumentException>(() => ScheduledOccurrenceCalendar.FindDueDates(definition, "UTC", Utc(2026, 1, 1), Utc(2026, 2, 2)));
            Assert.Throws<ArgumentException>(() => ScheduledOccurrenceCalendar.FindDueDates(definition, "UTC", new DateTime(2026, 1, 1), Utc(2026, 1, 2)));
            foreach (var zone in new[] { "", "Bad/Timezone", "Central America Standard Time" })
                Assert.Throws<ServiceException>(() => ScheduledOccurrenceCalendar.ResolveTimeZone(zone));
        }

        [Test]
        public void SnapshotCodecRequiresAnExplicitSupportedTaskType()
        {
            foreach (var json in new[] { "{}", "{\"taskType\":99}", "not-json", "null" })
                Assert.Throws<ServiceException>(() => ScheduledTaskDefinitionCodec.Read(json));
            var original = new TransferScheduledTaskVm { Id = Guid.NewGuid(), ToAccountId = 2, Days = new[] { 0 } };
            var decoded = ScheduledTaskDefinitionCodec.Read(ScheduledTaskDefinitionCodec.Write(original));
            Assert.That(decoded, Is.TypeOf<TransferScheduledTaskVm>());
            Assert.That(decoded.Id, Is.EqualTo(original.Id));
        }
    }
}
