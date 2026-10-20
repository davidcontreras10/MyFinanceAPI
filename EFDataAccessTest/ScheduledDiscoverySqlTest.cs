using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public partial class ScheduledExecutionSqlTest
    {
        private static DateTimeOffset Instant(int month, int day, int hour = 0) => new(2026, month, day, hour, 0, 0, TimeSpan.Zero);

        private static EFScheduledScheduleRepository Schedules(MyFinanceContext context, TimeProvider clock,
            bool allowed = true, bool historyEnabled = true)
        {
            var access = DispatchProxy.Create<IAccountAccessRepository, ScheduleAccess>();
            ((ScheduleAccess)(object)access).Allowed = allowed;
            return new EFScheduledScheduleRepository(context, clock, access,
                new ScheduledExecutionOptions { HistoryEnabled = historyEnabled });
        }

        private async Task<Guid> NewScheduledTask(ScheduledTaskFrequencyType frequency, params int[] days)
        {
            await using var context = Context();
            var id = Guid.NewGuid();
            context.Add(new AutomaticTask
            {
                AutomaticTaskId = id, UserId = _owner, TaskDescription = "Original task", Amount = 10,
                AccountId = 1, CurrencyId = 1, SpendTypeId = 3, PeriodTypeId = (int)frequency,
                Days = string.Join(',', days),
                SpInTrxDef = new SpInTrxDef { SpInTrxDefId = id, IsSpendTrx = true }
            });
            await context.SaveChangesAsync();
            return id;
        }

        private async Task Activate(Guid task, DateTimeOffset start, TimeProvider clock, string zone = ScheduledOccurrenceCalendar.DefaultTimeZone)
        {
            await using var context = Context();
            await Schedules(context, clock).ActivateAsync(task, _admin, start, zone);
        }

        private async Task<ScheduledDiscoveryResult> Discover(Guid task, TimeProvider clock)
        {
            await using var context = Context();
            return await Schedules(context, clock).DiscoverAsync(task, _owner);
        }

        [Test]
        public async Task DiscoveryActivationBoundaryPreventsHistoricalReplayAndCannotBeReset()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Weekly, Enumerable.Range(0, 7).ToArray());
            var clock = new ScheduleClock(Instant(1, 20, 12));
            var activation = Instant(1, 14, 12);
            await Activate(task, activation, clock);
            await Activate(task, activation, clock);
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(6));
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.Zero);
            await using var context = Context();
            Assert.That(await context.Set<ScheduledTaskOccurrence>().Where(x => x.TaskId == task).MinAsync(x => x.ScheduledLocalDate),
                Is.EqualTo(new DateTime(2026, 1, 15)));
            var error = Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).ActivateAsync(task, _admin, activation.AddDays(-1), "America/Costa_Rica"));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task DiscoveryCatchUpIsBoundedAndPreservesEveryMissedLocalDate()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Weekly, Enumerable.Range(0, 7).ToArray());
            var clock = new ScheduleClock(Instant(3, 10, 12));
            await Activate(task, Instant(1, 1), clock);
            var count = 0; var batches = 0;
            ScheduledDiscoveryResult result;
            do
            {
                result = await Discover(task, clock);
                count += result.AddedOccurrences;
                Assert.That(++batches, Is.LessThanOrEqualTo(4));
                Assert.That(result.AddedOccurrences, Is.LessThanOrEqualTo(32));
            } while (result.HasMore);
            Assert.That(count, Is.EqualTo(69));
            Assert.That(batches, Is.EqualTo(3));
            await using var context = Context();
            var pending = await Schedules(context, clock).GetPendingAsync(_owner, 100);
            Assert.That(pending.Count(x => x.TaskId == task), Is.EqualTo(1));
            Assert.That(pending.Single(x => x.TaskId == task).DueAtUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(pending.Single(x => x.TaskId == task).ScheduledLocalDate, Is.EqualTo(new DateTime(2026, 1, 1)));
            context.Add(new ScheduledTaskCompletion
            {
                Id = Guid.NewGuid(), TaskId = task, ActorId = _owner, OccurrenceId = pending.Single(x => x.TaskId == task).Id,
                PostingDate = new DateTime(2026, 1, 1), CompletedAtUtc = clock.GetUtcNow().UtcDateTime
            });
            await context.SaveChangesAsync();
            Assert.That((await Schedules(context, clock).GetPendingAsync(_owner, 100)).Single(x => x.TaskId == task).ScheduledLocalDate,
                Is.EqualTo(new DateTime(2026, 1, 2)));
        }

        [Test]
        public async Task DiscoveryConcurrentCallsDoNotDuplicateDatesOrAdvanceTwice()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            await Activate(task, Instant(1, 1), clock);
            var results = await Task.WhenAll(Discover(task, clock), Discover(task, clock));
            Assert.That(results.Sum(x => x.AddedOccurrences), Is.EqualTo(1));
            Assert.That(results.Select(x => x.NextScanUtc).Distinct().Count(), Is.EqualTo(1));
        }

        [Test]
        public async Task DiscoveryFailureRollsBackBothDatesAndProgress()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            var start = Instant(1, 1);
            await Activate(task, start, clock);
            await using (var failure = new CompletionFailureContext(new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options))
                Assert.ThrowsAsync<InvalidOperationException>(() => Schedules(failure, clock).DiscoverAsync(task, _owner));
            await using (var context = Context())
            {
                Assert.That(await context.Set<ScheduledTaskOccurrence>().AnyAsync(x => x.TaskId == task), Is.False);
                Assert.That((await context.Set<ScheduledTaskSchedule>().SingleAsync(x => x.TaskId == task)).NextScanUtc, Is.EqualTo(start.UtcDateTime));
            }
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(1));
        }

        [Test]
        public async Task DiscoveryUnscannedDueWorkKeepsItsOriginalDefinitionAfterAnEdit()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            var clock = new ScheduleClock(Instant(1, 29, 12));
            await Activate(task, Instant(1, 1), clock);
            await using (var context = Context())
            {
                var edits = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
                await Schedules(context, clock).ReviseAsync(task, _owner, () => edits.EditScheduledTaskAsync(new ClientEditScheduledTask
                {
                    TaskId = task.ToString(), Amount = 20, Days = new[] { 15 },
                    ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.Amount, ClientEditScheduledTask.ScheduledTaskField.Days }
                }, new ScheduledTaskEditScope(_owner, task)));
            }
            clock.Now = Instant(1, 30, 12);
            await Discover(task, clock);
            clock.Now = Instant(2, 16, 12);
            await Discover(task, clock);
            await using (var context = Context())
            {
                var dates = await context.Set<ScheduledTaskOccurrence>().Where(x => x.TaskId == task).OrderBy(x => x.ScheduledLocalDate).ToListAsync();
                Assert.That(dates.Select(x => x.ScheduledLocalDate), Is.EqualTo(new[] { new DateTime(2026, 1, 28), new DateTime(2026, 2, 15) }));
                Assert.That(dates.Select(x => ScheduledTaskDefinitionCodec.Read(x.DefinitionJson).Amount), Is.EqualTo(new[] { 10f, 20f }));
                Assert.That(await context.Set<ScheduledTaskRevision>().CountAsync(x => x.TaskId == task), Is.EqualTo(2));
            }
        }

        [Test]
        public async Task DiscoveryTimezoneMovingBackwardDoesNotRepeatAnAlreadyDueDate()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Weekly, 0);
            var clock = new ScheduleClock(Instant(1, 4, 1));
            await Activate(task, Instant(1, 4), clock, "UTC");
            await using (var context = Context())
                await Schedules(context, clock).ReviseAsync(task, _owner, () => Task.CompletedTask, "America/Los_Angeles");
            clock.Now = Instant(1, 4, 12);
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(1));
            await using (var context = Context())
            {
                var occurrence = await context.Set<ScheduledTaskOccurrence>().SingleAsync(x => x.TaskId == task);
                Assert.That(occurrence.TimeZoneId, Is.EqualTo("UTC"));
                Assert.That(occurrence.DueAtUtc, Is.EqualTo(Instant(1, 4).UtcDateTime));
            }
            clock.Now = Instant(1, 11, 12);
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(1));
        }

        [Test]
        public async Task DiscoveryRevisionFailureRestoresTheOldTaskAndTimeline()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            await Activate(task, Instant(1, 1), clock);
            await using (var context = Context())
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => Schedules(context, clock).ReviseAsync(task, _owner, async () =>
                {
                    var entity = await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task);
                    entity.Amount = 99;
                    await context.SaveChangesAsync();
                    throw new InvalidOperationException("Injected edit failure");
                }));
            }
            await using (var context = Context())
            {
                Assert.That((await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task)).Amount, Is.EqualTo(10));
                Assert.That(await context.Set<ScheduledTaskRevision>().CountAsync(x => x.TaskId == task), Is.EqualTo(1));
                Assert.That((await context.Set<ScheduledTaskRevision>().SingleAsync(x => x.TaskId == task)).EffectiveUntilUtc, Is.Null);
            }
        }

        [Test]
        public async Task DiscoveryAuthorizationAndActivationMustBeExplicit()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            await using (var context = Context())
            {
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).DiscoverAsync(task, _owner));
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).ActivateAsync(task, _owner, Instant(1, 1), "UTC"));
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock, false).ActivateAsync(task, _admin, Instant(1, 1), "UTC"));
                Assert.That(await context.Set<ScheduledTaskSchedule>().AnyAsync(x => x.TaskId == task), Is.False);
            }
            await Activate(task, Instant(1, 1), clock);
            await using (var context = Context())
            {
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).DiscoverAsync(task, _other));
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).ReviseAsync(task, _other, () => throw new AssertionException("Unauthorized edit reached")));
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).GetSchedulesToScanAsync(_owner, 100));
                Assert.That(await Schedules(context, clock).GetSchedulesToScanAsync(_admin, 100), Does.Contain(task));
            }
            await Discover(task, clock);
            await using (var context = Context())
            {
                Assert.That((await Schedules(context, clock).GetPendingAsync(_other, 100)).Any(x => x.TaskId == task), Is.False);
                Assert.That(await Schedules(context, clock).GetSchedulesToScanAsync(_admin, 100), Does.Not.Contain(task));
            }
        }

        [Test]
        public async Task DiscoveryInvalidMonthlyDaysCannotActivateOrReplaceValidRevisions()
        {
            var invalid = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 29);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            Assert.ThrowsAsync<ServiceException>(() => Activate(invalid, Instant(1, 1), clock));
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            await Activate(task, Instant(1, 1), clock);
            await using var context = Context();
            Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock).ReviseAsync(task, _owner, async () =>
            {
                (await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task)).Days = "31";
                await context.SaveChangesAsync();
            }));
            Assert.That((await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task)).Days, Is.EqualTo("14"));
        }

        [Test]
        public async Task DiscoveryEditAtMidnightDoesNotRewriteAnAlreadyDueOccurrence()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 14, 6));
            await Activate(task, Instant(1, 1), clock);
            await using (var context = Context())
                await Schedules(context, clock).ReviseAsync(task, _owner, async () =>
                {
                    (await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task)).Amount = 25;
                    await context.SaveChangesAsync();
                });
            clock.Now = Instant(1, 14, 8);
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(1));
            await using var check = Context();
            var occurrence = await check.Set<ScheduledTaskOccurrence>().SingleAsync(x => x.TaskId == task);
            Assert.That(ScheduledTaskDefinitionCodec.Read(occurrence.DefinitionJson).Amount, Is.EqualTo(10));
        }

        [Test]
        public async Task DiscoveryTimezoneMovingForwardPreservesOldDatesAndFindsTheNewLocalDay()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Weekly, Enumerable.Range(0, 7).ToArray());
            var clock = new ScheduleClock(Instant(1, 4, 8));
            await Activate(task, Instant(1, 4), clock);
            await using (var context = Context())
                await Schedules(context, clock).ReviseAsync(task, _owner, () => Task.CompletedTask, "Pacific/Kiritimati");
            clock.Now = Instant(1, 4, 12);
            Assert.That((await Discover(task, clock)).AddedOccurrences, Is.EqualTo(2));
            await using var check = Context();
            var occurrences = await check.Set<ScheduledTaskOccurrence>().Where(x => x.TaskId == task).OrderBy(x => x.DueAtUtc).ToListAsync();
            Assert.That(occurrences.Select(x => x.ScheduledLocalDate), Is.EqualTo(new[] { new DateTime(2026, 1, 4), new DateTime(2026, 1, 5) }));
            Assert.That(occurrences.Select(x => x.TimeZoneId), Is.EqualTo(new[] { "America/Costa_Rica", "Pacific/Kiritimati" }));
        }

        [Test]
        public async Task DiscoverySwitchToManualPreservesOlderDueWorkButCreatesNoNewDates()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            await Activate(task, Instant(1, 1), clock);
            await using (var context = Context())
                await Schedules(context, clock).ReviseAsync(task, _owner, async () =>
                {
                    (await context.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == task)).PeriodTypeId = (int)ScheduledTaskFrequencyType.Manual;
                    await context.SaveChangesAsync();
                });
            clock.Now = Instant(2, 15, 12);
            var result = await Discover(task, clock);
            if (result.HasMore) Assert.That((await Discover(task, clock)).HasMore, Is.False);
            await using var check = Context();
            Assert.That(await check.Set<ScheduledTaskOccurrence>().CountAsync(x => x.TaskId == task), Is.EqualTo(1));
        }

        [Test]
        public async Task DiscoverySchemaRejectsTwoOpenRevisionsAndInvalidIntervals()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 14);
            var clock = new ScheduleClock(Instant(1, 15, 12));
            await Activate(task, Instant(1, 1), clock);
            await using var context = Context();
            context.Add(new ScheduledTaskRevision
            {
                Id = Guid.NewGuid(), TaskId = task, EffectiveFromUtc = Instant(1, 2).UtcDateTime,
                TimeZoneId = "UTC", DefinitionJson = "{}"
            });
            Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            context.ChangeTracker.Clear();
            context.Add(new ScheduledTaskRevision
            {
                Id = Guid.NewGuid(), TaskId = task, EffectiveFromUtc = Instant(1, 2).UtcDateTime,
                EffectiveUntilUtc = Instant(1, 1).UtcDateTime, TimeZoneId = "UTC", DefinitionJson = "{}"
            });
            Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        public class ScheduleAccess : DispatchProxy
        {
            public bool Allowed { get; set; } = true;
            protected override object Invoke(MethodInfo method, object[] args) => Task.FromResult(Allowed);
        }

        private sealed class ScheduleClock(DateTimeOffset instant) : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = instant;
            public override DateTimeOffset GetUtcNow() => Now;
        }
    }
}
