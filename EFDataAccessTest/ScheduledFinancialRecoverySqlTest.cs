using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceModel;
using NUnit.Framework;
using DbSpend = EFDataAccess.Models.Spend;

namespace EFDataAccessTest
{
    public sealed partial class ScheduledFinancialWorkflowSqlTest
    {
        private const string InjectedPostingFailureMessage = "Injected failure after scheduled financial posting.";
        private const string FinancialWorkFailureStage = "FinancialWork";
        private const string CompletionFailureStage = "PersistCompletion";

        [TestCase(false, false, TestName = "SpendHistoryFailureRollsBackAndSameRequestRetryCommitsOnce")]
        [TestCase(false, true, TestName = "SpendCompletionFailureRollsBackAndSameRequestRetryCommitsOnce")]
        [TestCase(true, false, TestName = "TransferHistoryFailureRollsBackAndSameRequestRetryCommitsOnce")]
        [TestCase(true, true, TestName = "TransferCompletionFailureRollsBackAndSameRequestRetryCommitsOnce")]
        public async Task LatePostingFailureRollsBackAndSameRequestRetryCommitsOnce(bool transfer, bool failCompletion)
        {
            var marker = "Scheduled recovery test " + Guid.NewGuid().ToString("N");
            var taskIds = new List<Guid>();
            try
            {
                PostingReferences posting;
                Guid taskId;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await FindPostingReferencesAsync(setup);
                    if (posting == null)
                        Assert.Ignore("The local copy has no owner with overlapping account periods, same-currency accounts, and an available category.");
                    taskId = await CreateManualTaskAsync(setup, posting, transfer, marker);
                    taskIds.Add(taskId);
                }

                var request = new ScheduledExecutionRequest(taskId, posting.OwnerId, null,
                    Guid.NewGuid(), posting.PostingDate);
                var scope = new ScheduledTaskExecutionScope(posting.OwnerId, posting.OwnerId, taskId);
                var expectedSpendCount = transfer ? 2 : 1;
                var expectedFailureStage = failCompletion ? CompletionFailureStage : FinancialWorkFailureStage;
                int[] rolledBackSpendIds;
                int expectedPeriodRows;

                await using (var failing = new PostingFailureContext(Options(), taskId, marker, failCompletion))
                {
                    var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
                        CreateExecutionService(failing).ExecuteAsync(request, scope));
                    Assert.That(error.Message, Is.EqualTo(InjectedPostingFailureMessage));
                    Assert.That(failing.ObservedCallerTransaction, Is.True);
                    Assert.That(failing.ObservedSpendIds, Has.Length.EqualTo(expectedSpendCount));
                    Assert.That(failing.ObservedPeriodRows, Is.GreaterThanOrEqualTo(expectedSpendCount));
                    Assert.That(failing.ObservedTransferLinks, Is.EqualTo(transfer ? 1 : 0));
                    Assert.That(failing.ObservedLegacyHistoryRows, Is.EqualTo(failCompletion ? 1 : 0));
                    rolledBackSpendIds = failing.ObservedSpendIds;
                    expectedPeriodRows = failing.ObservedPeriodRows;
                }

                Guid failedAttemptId;
                await using (var verification = new MyFinanceContext(Options()))
                {
                    Assert.That(await verification.Set<DbSpend>().AnyAsync(x =>
                        x.Description.Contains(marker) || rolledBackSpendIds.Contains(x.SpendId)), Is.False);
                    Assert.That(await verification.Set<SpendOnPeriod>().AnyAsync(x =>
                        rolledBackSpendIds.Contains(x.SpendId)), Is.False);
                    Assert.That(await verification.Set<EFAppTransfer>().AnyAsync(x =>
                        rolledBackSpendIds.Contains(x.SourceAppTrxId) || rolledBackSpendIds.Contains(x.DestinationAppTrxId)), Is.False);
                    Assert.That(await verification.ExecutedTask.AnyAsync(x => x.AutomaticTaskId == taskId), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskCompletion>().AnyAsync(x => x.TaskId == taskId), Is.False);

                    var failed = await verification.Set<ScheduledTaskExecutionAttempt>().AsNoTracking()
                        .SingleAsync(x => x.TaskId == taskId);
                    failedAttemptId = failed.Id;
                    Assert.That(failed.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Failed));
                    Assert.That(failed.AttemptNumber, Is.EqualTo(1));
                    Assert.That(failed.ManualRequestId, Is.EqualTo(request.ManualRequestId));
                    Assert.That(failed.FailureStage, Is.EqualTo(expectedFailureStage));
                    Assert.That(failed.FailureType, Is.EqualTo(nameof(InvalidOperationException)));
                    Assert.That(failed.FailureMessage, Is.Not.Null.And.Not.Empty);
                    Assert.That(failed.EndedAtUtc, Is.Not.Null);
                }

                await using (var recovered = new MyFinanceContext(Options()))
                {
                    var execution = CreateExecutionService(recovered);
                    var retry = await execution.ExecuteAsync(request, scope);
                    var replay = await execution.ExecuteAsync(request, scope);
                    Assert.That(retry.AlreadyCompleted, Is.False);
                    Assert.That(replay.AlreadyCompleted, Is.True);
                    Assert.That(replay.ExecutionId, Is.EqualTo(retry.ExecutionId));

                    recovered.ChangeTracker.Clear();
                    await AssertCommittedFinancialEffectsAsync(recovered, taskId, marker, expectedSpendCount);
                    var spendIds = await recovered.Set<DbSpend>().Where(x => x.Description.Contains(marker))
                        .Select(x => x.SpendId).ToArrayAsync();
                    Assert.That(await recovered.Set<SpendOnPeriod>().CountAsync(x => spendIds.Contains(x.SpendId)),
                        Is.EqualTo(expectedPeriodRows));
                    var attempts = await recovered.Set<ScheduledTaskExecutionAttempt>().AsNoTracking()
                        .Where(x => x.TaskId == taskId).OrderBy(x => x.AttemptNumber).ToArrayAsync();
                    Assert.That(attempts.Select(x => x.AttemptNumber), Is.EqualTo(new[] { 1, 2 }));
                    Assert.That(attempts.Select(x => x.Status), Is.EqualTo(new[]
                        { ScheduledTaskAttemptStatus.Failed, ScheduledTaskAttemptStatus.Succeeded }));
                    Assert.That(attempts[0].Id, Is.EqualTo(failedAttemptId));
                    Assert.That(attempts[0].FailureStage, Is.EqualTo(expectedFailureStage));
                    Assert.That(attempts.All(x => x.ManualRequestId == request.ManualRequestId && x.EndedAtUtc.HasValue), Is.True);
                    Assert.That(attempts[1].FailureStage, Is.Null);
                    var completion = await recovered.Set<ScheduledTaskCompletion>().AsNoTracking()
                        .SingleAsync(x => x.TaskId == taskId);
                    Assert.That(completion.Id, Is.EqualTo(retry.ExecutionId));
                    Assert.That(completion.ManualRequestId, Is.EqualTo(request.ManualRequestId));
                }
            }
            finally
            {
                await CleanupAsync(marker, taskIds);
            }
        }

        private sealed class PostingFailureContext(DbContextOptions<MyFinanceContext> options,
            Guid taskId, string marker, bool failCompletion) : MyFinanceContext(options)
        {
            public bool ObservedCallerTransaction { get; private set; }
            public int[] ObservedSpendIds { get; private set; } = Array.Empty<int>();
            public int ObservedPeriodRows { get; private set; }
            public int ObservedTransferLinks { get; private set; }
            public int ObservedLegacyHistoryRows { get; private set; }

            public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                var injectFailure = failCompletion
                    ? ChangeTracker.Entries<ScheduledTaskCompletion>().Any(x => x.State == EntityState.Added)
                    : ChangeTracker.Entries<ExecutedTask>().Any(x => x.State == EntityState.Added);
                if (injectFailure)
                {
                    // Read saved rows in the caller's transaction before forcing its rollback.
                    ObservedCallerTransaction = Database.CurrentTransaction != null;
                    ObservedSpendIds = await Set<DbSpend>().AsNoTracking()
                        .Where(x => x.Description.Contains(marker)).Select(x => x.SpendId).ToArrayAsync(cancellationToken);
                    ObservedPeriodRows = await Set<SpendOnPeriod>().AsNoTracking()
                        .CountAsync(x => ObservedSpendIds.Contains(x.SpendId), cancellationToken);
                    ObservedTransferLinks = await Set<EFAppTransfer>().AsNoTracking().CountAsync(x =>
                        ObservedSpendIds.Contains(x.SourceAppTrxId) && ObservedSpendIds.Contains(x.DestinationAppTrxId), cancellationToken);
                    ObservedLegacyHistoryRows = await ExecutedTask.AsNoTracking()
                        .CountAsync(x => x.AutomaticTaskId == taskId, cancellationToken);
                    throw new InvalidOperationException(InjectedPostingFailureMessage);
                }

                return await base.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
