using System;
using System.Collections.Generic;
using System.Linq;
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
using DbAccount = EFDataAccess.Models.Account;
using DbPeriod = EFDataAccess.Models.AccountPeriod;
using DbSpend = EFDataAccess.Models.Spend;
using DbUser = EFDataAccess.Models.AppUser;

namespace EFDataAccessTest
{
    public sealed partial class ScheduledFinancialWorkflowSqlTest
    {
        private const string DispatchFixtureTimeZone = "America/Costa_Rica";
        private const string DispatchAdministratorRole = "Admin";
        private const int DispatchFixtureBatchLimit = 10;
        private const int CatchUpOccurrenceCount = 3;
        private static readonly DateTime DispatchFirstDate = new(2026, 1, 29);
        private static readonly DateTime DispatchPeriodEnd = DispatchFirstDate.AddDays(6);

        [TestCase(false, TestName = "AutomaticSpendsCatchUpOldestFirstAndReplayWithoutDuplicates")]
        [TestCase(true, TestName = "AutomaticTransfersCatchUpOldestFirstAndReplayWithoutDuplicates")]
        public async Task AutomaticCatchUpPostsOneOldestDatePerBatch(bool transfer)
        {
            var marker = "Automatic catch-up test " + Guid.NewGuid().ToString("N");
            var taskIds = new List<Guid>();
            var accountIds = new List<int>();
            var administratorId = Guid.NewGuid();
            var clock = new FinancialDispatchClock(DispatchInstant(DispatchFirstDate.AddDays(-1)));
            try
            {
                PostingReferences posting;
                Guid taskId;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await CreateDispatchReferencesAsync(setup, marker, administratorId, accountIds);
                    taskId = await CreateFinancialTaskAsync(setup, posting, transfer, marker, daily: true);
                    taskIds.Add(taskId);
                    await DispatchSchedules(setup, clock).ActivateAsync(taskId, administratorId, clock.Now, DispatchFixtureTimeZone);
                }

                var expectedDates = Enumerable.Range(0, CatchUpOccurrenceCount)
                    .Select(day => DispatchFirstDate.AddDays(day)).ToArray();
                clock.Now = DispatchInstant(expectedDates.Last());
                for (var batch = 0; batch < expectedDates.Length; batch++)
                {
                    var result = await DispatchFixtureAsync(clock, administratorId, taskIds);
                    Assert.That(result.ExecutionsCompleted, Is.EqualTo(1));
                    Assert.That(result.FailedCount, Is.Zero);
                    Assert.That(result.OccurrencesAdded, Is.EqualTo(batch == 0 ? CatchUpOccurrenceCount : 0));
                    await using var verification = new MyFinanceContext(Options());
                    var completedDates = await verification.Set<ScheduledTaskCompletion>().AsNoTracking()
                        .Where(x => x.TaskId == taskId).OrderBy(x => x.PostingDate).Select(x => x.PostingDate).ToArrayAsync();
                    Assert.That(completedDates, Is.EqualTo(expectedDates.Take(batch + 1)));
                    var pending = await DispatchSchedules(verification, clock).GetPendingAsync(administratorId, DispatchFixtureBatchLimit);
                    if (batch + 1 < expectedDates.Length)
                        Assert.That(pending.Single().ScheduledLocalDate, Is.EqualTo(expectedDates[batch + 1]));
                    else
                        Assert.That(pending, Is.Empty);
                }

                var idle = await DispatchFixtureAsync(clock, administratorId, taskIds);
                Assert.That(idle.ExecutionsCompleted, Is.Zero);
                Assert.That(idle.OccurrencesAdded, Is.Zero);
                Assert.That(idle.FailedCount, Is.Zero);
                await using var replayContext = new MyFinanceContext(Options());
                var first = await replayContext.Set<ScheduledTaskCompletion>().AsNoTracking()
                    .Where(x => x.TaskId == taskId).OrderBy(x => x.PostingDate).FirstAsync();
                var replay = await CreateExecutionService(replayContext, clock).ExecuteAsync(
                    new ScheduledExecutionRequest(taskId, administratorId, first.OccurrenceId, null, null),
                    new ScheduledTaskExecutionScope(administratorId, posting.OwnerId, taskId, administratorDispatch: true));
                Assert.That(replay.AlreadyCompleted, Is.True);
                Assert.That(replay.ExecutionId, Is.EqualTo(first.Id));
                await AssertAutomaticFinancialRowsAsync(replayContext, posting, taskId, marker,
                    administratorId, expectedDates, transfer);
            }
            finally
            {
                await CleanupAsync(marker, taskIds);
                await CleanupDispatchReferencesAsync(accountIds, administratorId);
            }
        }

        [TestCase(false, TestName = "MissingSpendPeriodKeepsOldestPendingWhileOtherTaskRunsThenRecovers")]
        [TestCase(true, TestName = "MissingTransferDestinationPeriodRollsBackWhileOtherTaskRunsThenRecovers")]
        public async Task MissingPeriodPreservesPendingWorkAndDoesNotBlockOtherTasks(bool transfer)
        {
            var marker = "Automatic period test " + Guid.NewGuid().ToString("N");
            var blockedDescription = marker + " blocked";
            var healthyDescription = marker + " healthy";
            var taskIds = new List<Guid>();
            var accountIds = new List<int>();
            var administratorId = Guid.NewGuid();
            var clock = new FinancialDispatchClock(DispatchInstant(DispatchFirstDate.AddDays(-1)));
            try
            {
                PostingReferences posting;
                Guid blockedTask;
                Guid healthyTask;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await CreateDispatchReferencesAsync(setup, marker, administratorId, accountIds,
                        omitSourcePeriod: !transfer, omitDestinationPeriod: transfer);
                    blockedTask = await CreateFinancialTaskAsync(setup, posting, transfer, blockedDescription, daily: true);
                    taskIds.Add(blockedTask);
                    await DispatchSchedules(setup, clock).ActivateAsync(blockedTask, administratorId, clock.Now, DispatchFixtureTimeZone);
                    clock.Now = DispatchInstant(DispatchFirstDate);
                    var healthyPosting = transfer ? posting : posting with { SourceAccountId = posting.DestinationAccountId };
                    healthyTask = await CreateFinancialTaskAsync(setup, healthyPosting, false, healthyDescription, daily: true);
                    taskIds.Add(healthyTask);
                    await DispatchSchedules(setup, clock).ActivateAsync(healthyTask, administratorId, clock.Now, DispatchFixtureTimeZone);
                    clock.Now = DispatchInstant(DispatchFirstDate.AddDays(1));
                    var schedules = DispatchSchedules(setup, clock);
                    await schedules.DiscoverAsync(blockedTask, administratorId);
                    await schedules.DiscoverAsync(healthyTask, administratorId);
                    var pending = await schedules.GetPendingAsync(administratorId, DispatchFixtureBatchLimit);
                    Assert.That(pending.Select(x => x.TaskId), Is.EqualTo(new[] { blockedTask, healthyTask }));
                }

                var failedBatch = await DispatchFixtureAsync(clock, administratorId, taskIds);
                Assert.That(failedBatch.ExecutionsCompleted, Is.EqualTo(1));
                Assert.That(failedBatch.FailedCount, Is.EqualTo(1));
                Assert.That(failedBatch.Failures.Single().TaskId, Is.EqualTo(blockedTask));
                Assert.That(failedBatch.Failures.Single().Stage, Is.EqualTo("execution"));
                Guid oldestOccurrence;
                await using (var verification = new MyFinanceContext(Options()))
                {
                    var occurrences = await verification.Set<ScheduledTaskOccurrence>().AsNoTracking()
                        .Where(x => x.TaskId == blockedTask).OrderBy(x => x.ScheduledLocalDate).ToArrayAsync();
                    Assert.That(occurrences.Select(x => x.ScheduledLocalDate), Is.EqualTo(new[]
                        { DispatchFirstDate, DispatchFirstDate.AddDays(1) }));
                    oldestOccurrence = occurrences[0].Id;
                    var failed = await verification.Set<ScheduledTaskExecutionAttempt>().AsNoTracking()
                        .SingleAsync(x => x.TaskId == blockedTask);
                    Assert.That(failed.OccurrenceId, Is.EqualTo(oldestOccurrence));
                    Assert.That(failed.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Failed));
                    Assert.That(failed.FailureStage, Is.EqualTo(FinancialWorkFailureStage));
                    Assert.That(await verification.Set<DbSpend>().AnyAsync(x => x.Description.Contains(blockedDescription)), Is.False);
                    Assert.That(await verification.ExecutedTask.AnyAsync(x => x.AutomaticTaskId == blockedTask), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskCompletion>().AnyAsync(x => x.TaskId == blockedTask), Is.False);
                    await AssertAutomaticFinancialRowsAsync(verification, posting, healthyTask, healthyDescription,
                        administratorId, new[] { DispatchFirstDate.AddDays(1) }, transfer: false);

                    verification.AccountPeriod.Add(DispatchPeriod(transfer ? posting.DestinationAccountId : posting.SourceAccountId,
                        posting.CurrencyId));
                    await verification.SaveChangesAsync();
                }

                var recovered = await DispatchFixtureAsync(clock, administratorId, taskIds);
                Assert.That(recovered.ExecutionsCompleted, Is.EqualTo(1));
                Assert.That(recovered.FailedCount, Is.Zero);
                await using (var verification = new MyFinanceContext(Options()))
                {
                    Assert.That(await verification.Set<ScheduledTaskCompletion>().Where(x => x.TaskId == blockedTask)
                        .Select(x => x.PostingDate).ToArrayAsync(), Is.EqualTo(new[] { DispatchFirstDate }));
                    var pending = await DispatchSchedules(verification, clock).GetPendingAsync(administratorId, DispatchFixtureBatchLimit);
                    Assert.That(pending.Single().ScheduledLocalDate, Is.EqualTo(DispatchFirstDate.AddDays(1)));
                    var attempts = await verification.Set<ScheduledTaskExecutionAttempt>().AsNoTracking()
                        .Where(x => x.OccurrenceId == oldestOccurrence).OrderBy(x => x.AttemptNumber).ToArrayAsync();
                    Assert.That(attempts.Select(x => x.AttemptNumber), Is.EqualTo(new[] { 1, 2 }));
                    Assert.That(attempts.Select(x => x.Status), Is.EqualTo(new[]
                        { ScheduledTaskAttemptStatus.Failed, ScheduledTaskAttemptStatus.Succeeded }));
                }

                var caughtUp = await DispatchFixtureAsync(clock, administratorId, taskIds);
                Assert.That(caughtUp.ExecutionsCompleted, Is.EqualTo(1));
                Assert.That(caughtUp.FailedCount, Is.Zero);
                var idle = await DispatchFixtureAsync(clock, administratorId, taskIds);
                Assert.That(idle.ExecutionsCompleted, Is.Zero);
                Assert.That(idle.FailedCount, Is.Zero);
                await using var final = new MyFinanceContext(Options());
                await AssertAutomaticFinancialRowsAsync(final, posting, blockedTask, blockedDescription,
                    administratorId, new[] { DispatchFirstDate, DispatchFirstDate.AddDays(1) }, transfer, expectedFailures: 1);
                await AssertAutomaticFinancialRowsAsync(final, posting, healthyTask, healthyDescription,
                    administratorId, new[] { DispatchFirstDate.AddDays(1) }, transfer: false);
            }
            finally
            {
                await CleanupAsync(marker, taskIds);
                await CleanupDispatchReferencesAsync(accountIds, administratorId);
            }
        }

        private async Task<PostingReferences> CreateDispatchReferencesAsync(MyFinanceContext context, string marker,
            Guid administratorId, List<int> accountIds, bool omitSourcePeriod = false, bool omitDestinationPeriod = false)
        {
            Assert.That(await context.Set<ScheduledTaskSchedule>().AnyAsync(), Is.False,
                "Automatic workflow tests require a copy without other activated schedules.");
            Assert.That(await context.Set<ScheduledTaskOccurrence>().AnyAsync(), Is.False,
                "Automatic workflow tests require a copy without another occurrence backlog.");
            var reference = await FindPostingReferencesAsync(context);
            if (reference == null)
                Assert.Ignore("The local copy has no owner/account/category references for financial workflow tests.");
            var role = await context.AppRoles.SingleAsync(x => x.Name == DispatchAdministratorRole);
            var administrator = new DbUser
            {
                UserId = administratorId, Username = administratorId.ToString("N"), Name = "Scheduled dispatch test",
                Password = "not-a-real-password"
            };
            administrator.UserRoles.Add(role);
            context.AppUser.Add(administrator);
            var templates = await context.Account.AsNoTracking().Where(x =>
                x.AccountId == reference.SourceAccountId || x.AccountId == reference.DestinationAccountId).ToArrayAsync();
            var source = CopyDispatchAccount(templates.Single(x => x.AccountId == reference.SourceAccountId), marker + " source", reference);
            var destination = CopyDispatchAccount(templates.Single(x => x.AccountId == reference.DestinationAccountId), marker + " destination", reference);
            context.Account.AddRange(source, destination);
            await context.SaveChangesAsync();
            accountIds.AddRange(new[] { source.AccountId, destination.AccountId });
            if (!omitSourcePeriod) context.AccountPeriod.Add(DispatchPeriod(source.AccountId, reference.CurrencyId));
            if (!omitDestinationPeriod) context.AccountPeriod.Add(DispatchPeriod(destination.AccountId, reference.CurrencyId));
            await context.SaveChangesAsync();
            return reference with { SourceAccountId = source.AccountId, DestinationAccountId = destination.AccountId, PostingDate = DispatchFirstDate };
        }

        private static DbAccount CopyDispatchAccount(DbAccount template, string name, PostingReferences posting) => new()
        {
            Name = name, UserId = posting.OwnerId, CurrencyId = posting.CurrencyId, DefaultSelectCurrencyId = posting.CurrencyId,
            AccountTypeId = template.AccountTypeId, AccountGroupId = template.AccountGroupId,
            FinancialEntityId = template.FinancialEntityId, PeriodDefinitionId = template.PeriodDefinitionId,
            DefaultSpendTypeId = posting.CategoryId, BaseBudget = 0
        };

        private static DbPeriod DispatchPeriod(int accountId, int currencyId) => new()
        {
            AccountId = accountId, CurrencyId = currencyId, InitialDate = DispatchFirstDate, EndDate = DispatchPeriodEnd, Budget = 0
        };

        private EFScheduledScheduleRepository DispatchSchedules(MyFinanceContext context, TimeProvider clock) => new(context, clock,
            new EFAccountRepository(context, NullLogger<EFAccountRepository>.Instance),
            new ScheduledExecutionOptions { Enabled = true, HistoryEnabled = true });

        private async Task<ScheduledTaskDispatchResult> DispatchFixtureAsync(TimeProvider clock, Guid administratorId, IReadOnlyCollection<Guid> taskIds)
        {
            await using var context = new MyFinanceContext(Options());
            Assert.That(await context.Set<ScheduledTaskSchedule>().AnyAsync(x => !taskIds.Contains(x.TaskId)), Is.False,
                "Refusing to dispatch a schedule outside this test.");
            Assert.That(await context.Set<ScheduledTaskOccurrence>().AnyAsync(x => !taskIds.Contains(x.TaskId)), Is.False,
                "Refusing to dispatch an occurrence outside this test.");
            var dispatcher = new ScheduledTaskDispatcher(DispatchSchedules(context, clock), CreateExecutionService(context, clock),
                new ScheduledExecutionOptions { Enabled = true, HistoryEnabled = true },
                new ScheduledTaskDispatchOptions { Enabled = true }, NullLogger<ScheduledTaskDispatcher>.Instance);
            return await dispatcher.DispatchDueAsync(administratorId);
        }

        private static async Task AssertAutomaticFinancialRowsAsync(MyFinanceContext context, PostingReferences posting,
            Guid taskId, string description, Guid administratorId, IReadOnlyCollection<DateTime> expectedDates,
            bool transfer, int expectedFailures = 0)
        {
            var occurrences = await context.Set<ScheduledTaskOccurrence>().AsNoTracking().Where(x => x.TaskId == taskId)
                .OrderBy(x => x.ScheduledLocalDate).ToArrayAsync();
            Assert.That(occurrences.Select(x => x.ScheduledLocalDate), Is.EqualTo(expectedDates));
            Assert.That(occurrences.All(x => x.TimeZoneId == DispatchFixtureTimeZone &&
                x.DueAtUtc == DispatchInstant(x.ScheduledLocalDate, 0).UtcDateTime), Is.True);
            var completions = await context.Set<ScheduledTaskCompletion>().AsNoTracking().Where(x => x.TaskId == taskId)
                .OrderBy(x => x.PostingDate).ToArrayAsync();
            Assert.That(completions.Select(x => x.PostingDate), Is.EqualTo(expectedDates));
            Assert.That(completions.All(x => x.ActorId == administratorId && x.OccurrenceId.HasValue && !x.ManualRequestId.HasValue), Is.True);
            var attempts = await context.Set<ScheduledTaskExecutionAttempt>().AsNoTracking().Where(x => x.TaskId == taskId).ToArrayAsync();
            Assert.That(attempts.Count(x => x.Status == ScheduledTaskAttemptStatus.Succeeded), Is.EqualTo(expectedDates.Count));
            Assert.That(attempts.Count(x => x.Status == ScheduledTaskAttemptStatus.Failed), Is.EqualTo(expectedFailures));
            Assert.That(attempts.Length, Is.EqualTo(expectedDates.Count + expectedFailures));
            Assert.That(attempts.All(x => x.ActorId == administratorId && x.OwnerId == posting.OwnerId &&
                x.OccurrenceId.HasValue && !x.ManualRequestId.HasValue && x.EndedAtUtc.HasValue), Is.True);
            var history = await context.ExecutedTask.AsNoTracking().Where(x => x.AutomaticTaskId == taskId)
                .OrderBy(x => x.ExecuteDatetime).ToArrayAsync();
            Assert.That(history.Select(x => x.ExecuteDatetime.Date), Is.EqualTo(expectedDates));
            Assert.That(history.All(x => x.ExecutionStatus == (int)ExecutedTaskStatus.Succeeded), Is.True);
            var spends = await context.Set<DbSpend>().AsNoTracking().Where(x => x.Description.Contains(description)).ToArrayAsync();
            Assert.That(spends.Length, Is.EqualTo(expectedDates.Count * (transfer ? 2 : 1)));
            foreach (var date in expectedDates)
            {
                var daily = spends.Where(x => x.SpendDate == date).ToArray();
                Assert.That(daily.Count(x => x.AmountTypeId == (int)TransactionTypeIds.Spend), Is.EqualTo(1));
                Assert.That(daily.Count(x => x.AmountTypeId == (int)TransactionTypeIds.Saving), Is.EqualTo(transfer ? 1 : 0));
            }
            var spendIds = spends.Select(x => x.SpendId).ToArray();
            var entries = await context.Set<SpendOnPeriod>().AsNoTracking().Where(x => spendIds.Contains(x.SpendId)).ToArrayAsync();
            Assert.That(entries.Length, Is.EqualTo(spends.Length));
            Assert.That(entries.All(x => x.IsOriginal == true), Is.True);
            Assert.That(await context.Set<EFAppTransfer>().CountAsync(x => spendIds.Contains(x.SourceAppTrxId) &&
                spendIds.Contains(x.DestinationAppTrxId)), Is.EqualTo(transfer ? expectedDates.Count : 0));
        }

        private async Task CleanupDispatchReferencesAsync(IReadOnlyCollection<int> accountIds, Guid administratorId)
        {
            await using var context = new MyFinanceContext(Options());
            await using var transaction = await context.Database.BeginTransactionAsync();
            context.AccountPeriod.RemoveRange(await context.AccountPeriod.Where(x => x.AccountId.HasValue &&
                accountIds.Contains(x.AccountId.Value)).ToArrayAsync());
            context.Account.RemoveRange(await context.Account.Where(x => accountIds.Contains(x.AccountId)).ToArrayAsync());
            var administrator = await context.AppUser.Include(x => x.UserRoles).SingleOrDefaultAsync(x => x.UserId == administratorId);
            if (administrator != null)
            {
                administrator.UserRoles.Clear();
                context.AppUser.Remove(administrator);
            }
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            Assert.That(await context.Account.AnyAsync(x => accountIds.Contains(x.AccountId)), Is.False);
            Assert.That(await context.AccountPeriod.AnyAsync(x => x.AccountId.HasValue && accountIds.Contains(x.AccountId.Value)), Is.False);
            Assert.That(await context.AppUser.AnyAsync(x => x.UserId == administratorId), Is.False);
        }

        private static DateTimeOffset DispatchInstant(DateTime date, int hour = 12) => new(TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.Date.AddHours(hour), DateTimeKind.Unspecified),
            ScheduledOccurrenceCalendar.ResolveTimeZone(DispatchFixtureTimeZone)));

        private sealed class FinancialDispatchClock(DateTimeOffset now) : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = now;
            public override DateTimeOffset GetUtcNow() => Now;
        }
    }
}
