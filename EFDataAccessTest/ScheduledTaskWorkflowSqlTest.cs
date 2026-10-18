using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
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
        private EFScheduledExecutionRepository WorkflowExecution(MyFinanceContext context, TimeProvider clock) =>
            new(context, ContextOptions(), clock, DispatchProxy.Create<IAutomaticTaskRepository, Definitions>(),
                NullLogger<EFScheduledExecutionRepository>.Instance);
        private ScheduledTaskAccessScope View(Guid task) => new(_owner, task, ResourceActionNames.View);
        private ScheduledTaskAccessScope Delete(Guid task) => new(_owner, task, ResourceActionNames.Delete);
        private async Task<Guid> WorkflowTask(ScheduleClock clock, int activationDay = 19)
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Weekly, Enumerable.Range(0, 7).ToArray());
            await Activate(task, Instant(1, activationDay, 12), clock);
            return task;
        }

        [Test]
        public async Task ContextManualExtraAndStaleTodayConfirmationRemainIndependentAndDuplicateSafe()
        {
            var clock = new ScheduleClock(Instant(1, 20, 12)); var task = await WorkflowTask(clock);
            ScheduledTaskExecutionContext initial;
            await using (var context = Context()) initial = await Schedules(context, clock).GetExecutionContextAsync(View(task));
            Assert.That(initial.LocalDate, Is.EqualTo(new DateTime(2026, 1, 20)));
            Assert.That(initial.DefaultAction, Is.EqualTo(ScheduledManualAction.RunTodayOccurrence));
            Assert.That(initial.Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Pending));
            Assert.That(initial.EarliestPending.Id, Is.EqualTo(initial.Today.Id));
            var extra = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), initial.LocalDate);
            var marker = Guid.NewGuid();
            await using (var context = Context()) await WorkflowExecution(context, clock).ExecuteAsync(extra, (_, _) => Probe(context, marker));
            await using (var context = Context())
                Assert.That((await Schedules(context, clock).GetExecutionContextAsync(View(task))).Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Pending));
            var scheduled = new ScheduledExecutionRequest(task, _admin, initial.Today.Id, null, null);
            ScheduledExecutionResult completed;
            await using (var context = Context()) completed = await WorkflowExecution(context, clock).ExecuteAsync(scheduled, (_, _) => Probe(context, marker));
            await using (var context = Context())
            {
                var stale = await WorkflowExecution(context, clock).ExecuteAsync(scheduled with { ActorId = _owner },
                    (_, _) => throw new AssertionException("Stale confirmation executed again"));
                Assert.That(stale.AlreadyCompleted, Is.True); Assert.That(stale.ExecutionId, Is.EqualTo(completed.ExecutionId));
            }
            await using (var context = Context())
            {
                var summary = await Schedules(context, clock).GetExecutionContextAsync(View(task));
                Assert.That(summary.Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Completed));
                Assert.That(summary.DefaultAction, Is.EqualTo(ScheduledManualAction.ExtraExecution));
                Assert.That(summary.WarnExtraExecution, Is.True);
            }
            await using (var context = Context()) await WorkflowExecution(context, clock).ExecuteAsync(extra with { ManualRequestId = Guid.NewGuid() }, (_, _) => Probe(context, marker));
            Assert.That(await ProbeCount(marker), Is.EqualTo(3));
            await using var history = Context();
            var page = await Schedules(history, clock).GetExecutionHistoryAsync(View(task), 0, 2);
            Assert.That(page.Items.Count, Is.EqualTo(2)); Assert.That(page.HasMore, Is.True);
            Assert.That((await Schedules(history, clock).GetExecutionHistoryAsync(View(task), 2, 2)).Items.Count, Is.EqualTo(1));
            Assert.That(page.Items.All(x => x.EventAtUtc.Kind == DateTimeKind.Utc), Is.True);
        }

        [Test]
        public async Task ContextBoundsCatchUpAndDoesNotDefaultToExtraWhileTodayIsUnknownOrBlocked()
        {
            var clock = new ScheduleClock(Instant(3, 10, 12)); var task = await WorkflowTask(clock, 1);
            await using var context = Context();
            var schedules = Schedules(context, clock);
            var first = await schedules.GetExecutionContextAsync(View(task));
            Assert.That(first.DiscoveryIncomplete, Is.True); Assert.That(first.DefaultAction, Is.Null);
            Assert.That(first.WarnExtraExecution, Is.True);
            ScheduledTaskExecutionContext current = first;
            for (var i = 0; i < 3 && current.DiscoveryIncomplete; i++) current = await schedules.GetExecutionContextAsync(View(task));
            Assert.That(current.DiscoveryIncomplete, Is.False);
            Assert.That(current.Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Pending));
            Assert.That(current.EarliestPending.ScheduledLocalDate, Is.LessThan(current.LocalDate));
            Assert.That(current.DefaultAction, Is.Null);
            var error = Assert.ThrowsAsync<ServiceException>(() => WorkflowExecution(context, clock).ExecuteAsync(
                new(task, _owner, current.Today.Id, null, null), (_, _) => throw new AssertionException("Overtook older work")));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [TestCase(ScheduledTaskFrequencyType.Monthly)]
        [TestCase(ScheduledTaskFrequencyType.Manual)]
        public async Task NotDueAndManualOnlyTasksOfferIndependentExtra(ScheduledTaskFrequencyType frequency)
        {
            var clock = new ScheduleClock(Instant(1, 20, 12));
            var task = await NewScheduledTask(frequency, frequency == ScheduledTaskFrequencyType.Manual ? Array.Empty<int>() : new[] { 28 });
            await Activate(task, Instant(1, 19, 12), clock);
            await using var context = Context();
            var summary = await Schedules(context, clock).GetExecutionContextAsync(View(task));
            Assert.That(summary.Today, Is.Null); Assert.That(summary.EarliestPending, Is.Null);
            Assert.That(summary.DefaultAction, Is.EqualTo(ScheduledManualAction.ExtraExecution));
            Assert.That(summary.WarnExtraExecution, Is.False);
        }

        [Test]
        public async Task RetirementCancelsPendingWorkPreservesHistoryAndCannotBeReactivated()
        {
            var clock = new ScheduleClock(Instant(1, 20, 12)); var task = await WorkflowTask(clock, 18);
            Guid completedOccurrence; Guid canceledOccurrence;
            await using (var context = Context())
            {
                var summary = await Schedules(context, clock).GetExecutionContextAsync(View(task));
                completedOccurrence = summary.EarliestPending.Id; canceledOccurrence = summary.Today.Id;
                await WorkflowExecution(context, clock).ExecuteAsync(new(task, _owner, completedOccurrence, null, null), (_, _) => Task.CompletedTask);
                await Schedules(context, clock).RetireAsync(Delete(task));
            }
            await using (var context = Context())
            {
                var schedules = Schedules(context, clock);
                var summary = await schedules.GetExecutionContextAsync(View(task));
                Assert.That(summary.Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Canceled));
                Assert.That(summary.CanceledOccurrenceCount, Is.EqualTo(1)); Assert.That(summary.DefaultAction, Is.Null);
                Assert.That(summary.EarliestPending, Is.Null);
                Assert.That((await schedules.GetPendingAsync(_admin, 100)).Any(x => x.TaskId == task), Is.False);
                Assert.That((await schedules.GetSchedulesToScanAsync(_admin, 100)).Contains(task), Is.False);
                Assert.That((await schedules.DiscoverAsync(task, _owner)).AddedOccurrences, Is.Zero);
                var history = await schedules.GetExecutionHistoryAsync(View(task), 0, 100);
                Assert.That(history.Items.Select(x => x.Status), Is.EquivalentTo(new[] { ScheduledOccurrenceStatus.Completed, ScheduledOccurrenceStatus.Canceled }));
                Assert.That(Assert.ThrowsAsync<ServiceException>(() => WorkflowExecution(context, clock).ExecuteAsync(
                    new(task, _owner, canceledOccurrence, null, null), (_, _) => throw new AssertionException("Canceled work executed"))).StatusCode,
                    Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(Assert.ThrowsAsync<ServiceException>(() => WorkflowExecution(context, clock).ExecuteAsync(
                    new(task, _owner, null, Guid.NewGuid(), clock.Now.Date), (_, _) => throw new AssertionException("Retired extra executed"))).StatusCode,
                    Is.EqualTo(HttpStatusCode.Conflict));
                Assert.ThrowsAsync<ServiceException>(() => schedules.ActivateAsync(task, _admin, Instant(1, 18, 12), "America/Costa_Rica"));
                Assert.ThrowsAsync<ServiceException>(() => schedules.ReviseAsync(task, _owner, () => throw new AssertionException("Retired edit reached")));
                var receipt = await WorkflowExecution(context, clock).ExecuteAsync(new(task, _owner, completedOccurrence, null, null),
                    (_, _) => throw new AssertionException("Completed receipt re-executed"));
                Assert.That(receipt.AlreadyCompleted, Is.True);
            }
            clock.Now = Instant(1, 21, 12);
            await using var verification = Context();
            await Schedules(verification, clock).RetireAsync(Delete(task));
            Assert.That((await verification.AutomaticTask.FindAsync(task)).RetiredAtUtc, Is.EqualTo(Instant(1, 20, 12).UtcDateTime));
            Assert.That(await verification.SpInTrxDef.AnyAsync(x => x.SpInTrxDefId == task), Is.True);
            Assert.That(await verification.Set<ScheduledTaskRevision>().CountAsync(x => x.TaskId == task), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task RetirementWaitsForInFlightCommitOrRollbackWithoutPartialEffects(bool fails)
        {
            var clock = new ScheduleClock(Instant(1, 20, 12)); var task = await WorkflowTask(clock); Guid occurrence;
            await using (var context = Context()) occurrence = (await Schedules(context, clock).GetExecutionContextAsync(View(task))).Today.Id;
            var request = new ScheduledExecutionRequest(task, _owner, occurrence, null, null);
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); var marker = Guid.NewGuid();
            async Task<ScheduledExecutionResult> Run()
            {
                await using var context = Context();
                return await WorkflowExecution(context, clock).ExecuteAsync(request, async (_, _) =>
                {
                    await Probe(context, marker); entered.TrySetResult(true);
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                    if (fails) throw new InvalidOperationException("Simulated in-flight failure");
                });
            }
            async Task Retire() { await using var context = Context(); await Schedules(context, clock).RetireAsync(Delete(task)); }
            var running = Run(); await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var retiring = Retire(); await Task.Delay(100);
            Assert.That(retiring.IsCompleted, Is.False); release.TrySetResult(true);
            if (fails) Assert.ThrowsAsync<InvalidOperationException>(() => running); else await running;
            await retiring;
            Assert.That(await ProbeCount(marker), Is.EqualTo(fails ? 0 : 1));
            await using var verification = Context();
            if (fails)
                Assert.ThrowsAsync<ServiceException>(() => WorkflowExecution(verification, clock).ExecuteAsync(request,
                    (_, _) => throw new AssertionException("Canceled retry executed")));
            else
                Assert.That((await WorkflowExecution(verification, clock).ExecuteAsync(request,
                    (_, _) => throw new AssertionException("Committed result executed twice"))).AlreadyCompleted, Is.True);
        }

        [Test]
        public async Task ForeignScopesCannotInspectOrRetireAndStaleExecutionScopeCannotWrite()
        {
            var clock = new ScheduleClock(Instant(1, 20, 12)); var task = await WorkflowTask(clock);
            await using var context = Context(); var schedules = Schedules(context, clock);
            Assert.ThrowsAsync<ServiceException>(() => schedules.GetExecutionContextAsync(new(_other, task, ResourceActionNames.View)));
            Assert.ThrowsAsync<ServiceException>(() => schedules.GetExecutionHistoryAsync(new(_other, task, ResourceActionNames.View), 0, 50));
            Assert.ThrowsAsync<ServiceException>(() => schedules.RetireAsync(new(_other, task, ResourceActionNames.Delete)));
            var error = Assert.ThrowsAsync<ServiceException>(() => WorkflowExecution(context, clock).ExecuteAsync(
                new(task, _owner, null, Guid.NewGuid(), clock.Now.Date, _other), (_, _) => throw new AssertionException("Stale owner scope executed")));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That((await context.AutomaticTask.FindAsync(task)).RetiredAtUtc, Is.Null);
        }

        [Test]
        public async Task LegacyTaskRetirementKeepsLegacyHistoryAndFiltersNormalListings()
        {
            var owner = await PreferenceOwner(); var refs = await CreationReferences(owner); var clock = new ScheduleClock(Instant(1, 20, 12));
            Guid task;
            await using (var context = Context())
            {
                var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
                task = await repository.CreateAsync(new(owner), new ClientScheduledTask.Basic
                {
                    AccountId = refs.Account, SpendTypeId = refs.Category, CurrencyId = refs.Currency, Amount = 10,
                    Description = "Legacy history task", FrequencyType = ScheduledTaskFrequencyType.Monthly, Days = new[] { 28 }
                });
                await repository.RecordClientExecutedTaskAsync(new ClientExecutedTask
                {
                    AutomaticTaskId = task.ToString(), ExecutedByUserId = owner.ToString(), ExecuteDatetime = clock.Now.DateTime,
                    ExecutionStatus = ExecutedTaskStatus.Succeeded, ExecutionMsg = "Preserved history"
                });
                Assert.That((await repository.GetScheduledByUserIdAsync(owner.ToString())).Count, Is.EqualTo(1));
                await Schedules(context, clock, historyEnabled: false).RetireAsync(new(owner, task, ResourceActionNames.Delete));
                Assert.That((await repository.GetScheduledByUserIdAsync(owner.ToString())).Count, Is.Zero);
                Assert.That((await repository.GetExecutedTasksByTaskIdAsync(new(owner, task, ResourceActionNames.View))).Count, Is.EqualTo(1));
                Assert.That((await repository.GetExecutedTasksByTaskIdAsync(new(_other, task, ResourceActionNames.View))).Count, Is.Zero);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LegacyFailureRollsBackFinancialProbe(bool returnedFailure)
        {
            var clock = new ScheduleClock(Instant(1, 20, 12));
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28); var marker = Guid.NewGuid();
            await using (var context = Context())
            {
                async Task<TaskExecutedResult> Failure()
                {
                    await Probe(context, marker);
                    if (returnedFailure) return TaskExecutedResult.Error("Simulated financial failure", task.ToString());
                    throw new InvalidOperationException("Simulated financial exception");
                }
                var repository = Schedules(context, clock, historyEnabled: false);
                if (returnedFailure) Assert.ThrowsAsync<ServiceException>(() => repository.ExecuteLegacyAsync(new(_owner, _owner, task), Failure));
                else Assert.ThrowsAsync<InvalidOperationException>(() => repository.ExecuteLegacyAsync(new(_owner, _owner, task), Failure));
            }
            Assert.That(await ProbeCount(marker), Is.Zero);
        }

        [Test]
        public async Task FailedRetirementLeavesPendingWorkAndHistoryUnchanged()
        {
            var clock = new ScheduleClock(Instant(1, 20, 12)); var task = await WorkflowTask(clock);
            await using (var context = Context()) await Schedules(context, clock).GetExecutionContextAsync(View(task));
            await using (var failure = new RetirementFailureContext(new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options))
                Assert.ThrowsAsync<InvalidOperationException>(() => Schedules(failure, clock).RetireAsync(Delete(task)));
            await using var verification = Context();
            var summary = await Schedules(verification, clock).GetExecutionContextAsync(View(task));
            Assert.That(summary.RetiredAtUtc, Is.Null); Assert.That(summary.Today.Status, Is.EqualTo(ScheduledOccurrenceStatus.Pending));
        }

        private sealed class RetirementFailureContext(DbContextOptions<MyFinanceContext> options) : LedgerTestContext(options)
        {
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                if (ChangeTracker.Entries<AutomaticTask>().Any(x => x.Property(task => task.RetiredAtUtc).IsModified))
                    throw new InvalidOperationException("Simulated retirement save failure");
                return base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
