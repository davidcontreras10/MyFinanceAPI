using System;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class ScheduledExecutionRulesTest
    {
        private static ScheduledExecutionRequest Manual() =>
            new(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), new DateTime(2026, 10, 14));

        [Test]
        public void ManualAndOccurrenceUseIndependentIdentities()
        {
            var request = Manual();
            Assert.DoesNotThrow(() => ScheduledExecutionRules.Validate(request));
            Assert.DoesNotThrow(() => ScheduledExecutionRules.Validate(request with
                { ManualRequestId = null, ManualPostingDate = null, OccurrenceId = Guid.NewGuid() }));
        }

        [Test]
        public void InvalidIdentityCombinationsFailClosed()
        {
            var request = Manual();
            foreach (var invalid in new[]
            {
                request with { ActorId = Guid.Empty }, request with { TaskId = Guid.Empty },
                request with { ManualRequestId = Guid.Empty }, request with { ManualRequestId = null },
                request with { OccurrenceId = Guid.NewGuid() }, request with { ManualPostingDate = null },
                request with { ManualRequestId = null, OccurrenceId = Guid.NewGuid() }
            }) Assert.Throws<ServiceException>(() => ScheduledExecutionRules.Validate(invalid));
            Assert.Throws<ServiceException>(() => ScheduledExecutionRules.Validate(null));
        }

        [TestCase(1)]
        [TestCase(28)]
        public void MonthlyAllowsOnlyValidDayRange(int day) =>
            Assert.DoesNotThrow(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Monthly, new[] { day }));

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(31)]
        public void MonthlyRejectsMonthEndDays(int day) =>
            Assert.Throws<ServiceException>(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Monthly, new[] { day }));

        [TestCase(0)]
        [TestCase(6)]
        public void WeeklyIncludesSunday(int day) =>
            Assert.DoesNotThrow(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Weekly, new[] { day }));

        [Test]
        public void WeeklyRejectsInvalidDaysAndEmptySchedules()
        {
            Assert.Throws<ServiceException>(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Weekly, new[] { 7 }));
            Assert.Throws<ServiceException>(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Weekly, null));
            Assert.Throws<ServiceException>(() => ScheduledExecutionRules.ValidateSchedule(ScheduledTaskFrequencyType.Invalid, new[] { 1 }));
        }
    }
}
