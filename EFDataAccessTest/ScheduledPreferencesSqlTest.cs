using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public partial class ScheduledExecutionSqlTest
    {
        private async Task<Guid> PreferenceOwner()
        {
            var owner = Guid.NewGuid();
            await using var context = Context();
            // Omit the new column to verify the Fluent API's database default.
            await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [AppUser] ([UserId], [Username], [Name], [Password]) VALUES ({owner}, {owner.ToString("N")}, {"Scheduling test"}, {"not-a-real-password"})");
            return owner;
        }

        private static async Task<Guid> PersistDailyTask(MyFinanceContext context, Guid owner)
        {
            var task = Guid.NewGuid();
            context.Add(new AutomaticTask
            {
                AutomaticTaskId = task, UserId = owner, TaskDescription = "Daily test", Amount = 10,
                AccountId = 1, CurrencyId = 1, SpendTypeId = 3,
                PeriodTypeId = (int)ScheduledTaskFrequencyType.Weekly, Days = "0,1,2,3,4,5,6",
                SpInTrxDef = new SpInTrxDef { SpInTrxDefId = task, IsSpendTrx = true }
            });
            await context.SaveChangesAsync();
            return task;
        }

        [Test]
        public async Task PreferencesDefaultPersistAndDoNotActivateLegacyTasks()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 28, 12));
            await using (var context = Context())
                await PersistDailyTask(context, owner);
            await using (var context = Context())
            {
                var repository = Schedules(context, clock);
                Assert.That((await repository.GetPreferencesAsync(owner)).TimeZoneId, Is.EqualTo("America/Costa_Rica"));
                await repository.UpdatePreferencesAsync(owner, "Asia/Kathmandu");
                Assert.That(Assert.ThrowsAsync<ServiceException>(() => repository.UpdatePreferencesAsync(owner, "invalid-zone")).StatusCode,
                    Is.EqualTo(HttpStatusCode.BadRequest));
            }
            await using var verification = Context();
            Assert.That((await Schedules(verification, clock).GetPreferencesAsync(owner)).TimeZoneId, Is.EqualTo("Asia/Kathmandu"));
            Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(x => x.OwnerId == owner), Is.False);
            Assert.That(Assert.ThrowsAsync<ServiceException>(() => Schedules(verification, clock).GetPreferencesAsync(Guid.NewGuid())).StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task ProfileTimezoneChangePreservesMaterializedAndUndiscoveredDueDates()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 27, 12));
            Guid task;
            await using (var context = Context()) task = await PersistDailyTask(context, owner);
            await using (var context = Context())
            {
                await Schedules(context, clock).ActivateAsync(task, _admin, Instant(1, 26, 12), "America/Costa_Rica");
                await Schedules(context, clock).DiscoverAsync(task, owner);
            }
            clock.Now = Instant(1, 28, 6); // Exactly the old timezone's midnight.
            await using (var context = Context())
            {
                await Schedules(context, clock).UpdatePreferencesAsync(owner, "America/New_York");
                await Schedules(context, clock).UpdatePreferencesAsync(owner, "America/New_York");
                Assert.That(await context.Set<ScheduledTaskRevision>().CountAsync(x => x.TaskId == task), Is.EqualTo(2));
            }
            clock.Now = Instant(1, 30, 12);
            await using (var context = Context()) await Schedules(context, clock).DiscoverAsync(task, owner);
            await using var verification = Context();
            var dates = await verification.Set<ScheduledTaskOccurrence>().Where(x => x.TaskId == task).OrderBy(x => x.ScheduledLocalDate).ToListAsync();
            Assert.That(dates.Select(x => x.ScheduledLocalDate.Day), Is.EqualTo(new[] { 27, 28, 29, 30 }));
            Assert.That(dates.Select(x => x.TimeZoneId), Is.EqualTo(new[] { "America/Costa_Rica", "America/Costa_Rica", "America/New_York", "America/New_York" }));
            Assert.That(dates.Select(x => x.DueAtUtc.Hour), Is.EqualTo(new[] { 6, 6, 5, 5 }));
        }

        [Test]
        public async Task FailedPreferenceSaveRollsBackAllTaskRevisionsAndTheProfile()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 28, 12));
            Guid first; Guid second;
            await using (var context = Context()) { first = await PersistDailyTask(context, owner); second = await PersistDailyTask(context, owner); }
            foreach (var task in new[] { first, second })
            {
                await using var context = Context();
                await Schedules(context, clock).ActivateAsync(task, _admin, Instant(1, 20), "America/Costa_Rica");
            }
            await using (var failure = new PreferenceFailureContext(new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options))
                Assert.ThrowsAsync<InvalidOperationException>(() => Schedules(failure, clock).UpdatePreferencesAsync(owner, "Asia/Kathmandu"));
            await using var verification = Context();
            Assert.That((await verification.AppUser.FindAsync(owner)).SchedulingTimeZoneId, Is.EqualTo("America/Costa_Rica"));
            var revisions = await verification.Set<ScheduledTaskRevision>().Where(x => x.TaskId == first || x.TaskId == second).ToListAsync();
            Assert.That(revisions.Count, Is.EqualTo(2));
            Assert.That(revisions.All(x => x.EffectiveUntilUtc == null && x.TimeZoneId == "America/Costa_Rica"), Is.True);
        }

        [Test]
        public async Task ConcurrentPreferenceChangesLeaveOneConsistentOpenRevisionPerTask()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 28, 12));
            Guid task;
            await using (var context = Context()) task = await PersistDailyTask(context, owner);
            await using (var context = Context()) await Schedules(context, clock).ActivateAsync(task, _admin, Instant(1, 20), "America/Costa_Rica");
            async Task Change(string zone) { await using var context = Context(); await Schedules(context, clock).UpdatePreferencesAsync(owner, zone); }
            await Task.WhenAll(Change("America/New_York"), Change("Asia/Kathmandu"));
            await using var verification = Context();
            var zone = (await verification.AppUser.FindAsync(owner)).SchedulingTimeZoneId;
            var revision = await verification.Set<ScheduledTaskRevision>().SingleAsync(x => x.TaskId == task && x.EffectiveUntilUtc == null);
            Assert.That(revision.TimeZoneId, Is.EqualTo(zone));
            Assert.That(await verification.Set<ScheduledTaskRevision>().CountAsync(x => x.TaskId == task), Is.EqualTo(3));
        }

        [Test]
        public async Task ConcurrentRegistrationAndPreferenceChangeCannotLeaveAStaleTimezone()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 20, 12));
            async Task<Guid> Register()
            {
                await using var context = Context();
                return await Schedules(context, clock).RegisterAsync(owner, () => PersistDailyTask(context, owner));
            }
            async Task Change()
            {
                await using var context = Context();
                await Schedules(context, clock).UpdatePreferencesAsync(owner, "Asia/Kathmandu");
            }
            var registration = Register();
            await Task.WhenAll(registration, Change());
            await using var verification = Context();
            var task = await registration;
            Assert.That((await verification.AppUser.FindAsync(owner)).SchedulingTimeZoneId, Is.EqualTo("Asia/Kathmandu"));
            Assert.That((await verification.Set<ScheduledTaskRevision>().SingleAsync(x => x.TaskId == task && x.EffectiveUntilUtc == null)).TimeZoneId,
                Is.EqualTo("Asia/Kathmandu"));
        }

        [Test]
        public async Task RegistrationValidationFailureRollsBackTheAlreadySavedTaskAndDefinition()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 20, 12)); var task = Guid.Empty;
            await using (var context = Context())
                Assert.ThrowsAsync<ServiceException>(() => Schedules(context, clock, allowed: false).RegisterAsync(owner, async () =>
                {
                    task = await PersistDailyTask(context, owner);
                    return task;
                }));
            await using var verification = Context();
            Assert.That(task, Is.Not.EqualTo(Guid.Empty));
            Assert.That(await verification.AutomaticTask.AnyAsync(x => x.AutomaticTaskId == task), Is.False);
            Assert.That(await verification.SpInTrxDef.AnyAsync(x => x.SpInTrxDefId == task), Is.False);
            Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(x => x.TaskId == task), Is.False);
        }

        private async Task<(int Account, int Destination, int Category, int Currency)> CreationReferences(Guid owner)
        {
            await using var context = Context();
            // Placeholder account prerequisites, not financial posting behavior, are under test here.
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE [Account] NOCHECK CONSTRAINT ALL");
            var source = new Account { UserId = owner, Name = "Creation source" };
            var destination = new Account { UserId = owner, Name = "Creation destination" };
            var category = new EFDataAccess.Models.SpendType { Name = "Creation category" };
            var currency = new Currency { Name = "Test currency", Symbol = "T", IsoCode = "ZZZ" };
            context.AddRange(source, destination, category, currency);
            await context.SaveChangesAsync();
            context.Add(new UserSpendType { UserId = owner, SpendTypeId = category.SpendTypeId });
            await context.SaveChangesAsync();
            return (source.AccountId, destination.AccountId, category.SpendTypeId, currency.CurrencyId);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task RealCreationPersistsOwnedDefinitionsAndRegistersOnlyWhenEnabled(bool transfer, bool history)
        {
            var owner = await PreferenceOwner(); var refs = await CreationReferences(owner);
            var clock = new ScheduleClock(Instant(1, 20, 12));
            await using (var context = Context()) await Schedules(context, clock).UpdatePreferencesAsync(owner, "Asia/Kathmandu");
            ClientScheduledTask request = transfer ? new ClientScheduledTask.Transfer { ToAccountId = refs.Destination } : new ClientScheduledTask.Basic { IsSpendTrx = true };
            request.AccountId = refs.Account; request.SpendTypeId = refs.Category; request.CurrencyId = refs.Currency;
            request.Amount = 10; request.Description = "New task"; request.FrequencyType = ScheduledTaskFrequencyType.Weekly; request.Days = new[] { 0 };
            await using (var context = Context())
            {
                var tasks = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
                var service = new ScheduledTasksService(tasks, null, null, null, null, Schedules(context, clock),
                    new ScheduledExecutionOptions { HistoryEnabled = history });
                var scope = new ScheduledTaskCreateScope(owner);
                if (transfer) await service.CreateTransferTrxAsync(scope, (ClientScheduledTask.Transfer)request);
                else await service.CreateBasicTrxAsync(scope, (ClientScheduledTask.Basic)request);
                var roundTrip = await tasks.GetScheduledByUserIdAsync(owner.ToString());
                Assert.That(roundTrip.Single().Days, Is.EqualTo(new[] { 0 }));
            }
            await using var verification = Context();
            var task = await verification.AutomaticTask.SingleAsync(x => x.UserId == owner);
            Assert.That(await verification.SpInTrxDef.CountAsync(x => x.SpInTrxDefId == task.AutomaticTaskId), Is.EqualTo(transfer ? 0 : 1));
            Assert.That(await verification.TransferTrxDef.CountAsync(x => x.TransferTrxDefId == task.AutomaticTaskId), Is.EqualTo(transfer ? 1 : 0));
            Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(x => x.TaskId == task.AutomaticTaskId), Is.EqualTo(history));
            if (history)
            {
                var state = await verification.Set<ScheduledTaskSchedule>().SingleAsync(x => x.TaskId == task.AutomaticTaskId);
                Assert.That(state.ActivatedAtUtc, Is.EqualTo(clock.Now.UtcDateTime));
                var revision = await verification.Set<ScheduledTaskRevision>().SingleAsync(x => x.TaskId == task.AutomaticTaskId);
                Assert.That(revision.TimeZoneId, Is.EqualTo("Asia/Kathmandu"));
                clock.Now = Instant(1, 27, 12);
                await Schedules(verification, clock).DiscoverAsync(task.AutomaticTaskId, owner);
                var occurrence = await verification.Set<ScheduledTaskOccurrence>().SingleAsync(x => x.TaskId == task.AutomaticTaskId);
                Assert.That(occurrence.ScheduledLocalDate, Is.EqualTo(new DateTime(2026, 1, 25)));
                Assert.That(occurrence.DueAtUtc.Hour, Is.EqualTo(18));
                Assert.That(occurrence.DueAtUtc.Minute, Is.EqualTo(15));
            }
        }

        [Test]
        public async Task ScopedCreationRechecksReferencesAndLegacyEditsRejectMonthly29()
        {
            var owner = await PreferenceOwner(); var refs = await CreationReferences(owner);
            await using var context = Context();
            var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            var request = new ClientScheduledTask.Basic
            {
                AccountId = refs.Account, SpendTypeId = refs.Category, CurrencyId = refs.Currency, Amount = 10,
                Description = "Monthly task", FrequencyType = ScheduledTaskFrequencyType.Monthly, Days = new[] { 28 }
            };
            Assert.That(Assert.ThrowsAsync<ServiceException>(() => repository.CreateAsync(new ScheduledTaskCreateScope(_other), request)).StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound));
            var task = await repository.CreateAsync(new ScheduledTaskCreateScope(owner), request);
            Assert.ThrowsAsync<ServiceException>(() => repository.EditScheduledTaskAsync(new ClientEditScheduledTask
            {
                Days = new[] { 29 }, ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.Days }
            }, new ScheduledTaskEditScope(owner, task)));
            await using var verification = Context();
            Assert.That((await verification.AutomaticTask.FindAsync(task)).Days, Is.EqualTo("28"));
        }

        [Test]
        public async Task RegisteredManualTaskHasNoAutomaticOccurrences()
        {
            var owner = await PreferenceOwner(); var clock = new ScheduleClock(Instant(1, 20, 12));
            Guid task;
            await using (var context = Context())
                task = await Schedules(context, clock).RegisterAsync(owner, async () =>
                {
                    var id = await PersistDailyTask(context, owner);
                    var entity = await context.AutomaticTask.FindAsync(id);
                    entity.PeriodTypeId = (int)ScheduledTaskFrequencyType.Manual;
                    entity.Days = "";
                    await context.SaveChangesAsync();
                    return id;
                });
            clock.Now = Instant(2, 20, 12);
            await using var verification = Context();
            Assert.That((await Schedules(verification, clock).DiscoverAsync(task, owner)).AddedOccurrences, Is.Zero);
        }

        private sealed class PreferenceFailureContext(DbContextOptions<MyFinanceContext> options) : LedgerTestContext(options)
        {
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (ChangeTracker.Entries<EFDataAccess.Models.AppUser>().Any(x => x.Property(u => u.SchedulingTimeZoneId).IsModified))
                    throw new InvalidOperationException("Simulated preference save failure after revision writes.");
                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
