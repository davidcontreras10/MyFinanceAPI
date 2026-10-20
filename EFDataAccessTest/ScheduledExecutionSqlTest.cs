using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;
using AppUser = EFDataAccess.Models.AppUser;

namespace EFDataAccessTest
{
    [Category("LocalSql")]
    [NonParallelizable]
    public partial class ScheduledExecutionSqlTest
    {
        private string _connection;
        private readonly Guid _owner = Guid.NewGuid();
        private readonly Guid _other = Guid.NewGuid();
        private readonly Guid _admin = Guid.NewGuid();

        [OneTimeSetUp]
        public async Task CreateIsolatedDatabase()
        {
            if (Environment.GetEnvironmentVariable("MYFINANCE_RUN_SQL_TESTS") != "1")
                Assert.Ignore("Set MYFINANCE_RUN_SQL_TESTS=1 to run against a new, disposable LocalDB database.");

            var name = "MyFinance_ScheduledExecutionTests_" + Guid.NewGuid().ToString("N");
            _connection = $"Server=(localdb)\\MSSQLLocalDB;Database={name};Integrated Security=True;TrustServerCertificate=True";
            await using var context = Context();
            await context.Database.EnsureCreatedAsync();
            // This suite tests the execution ledger, not the existing financial schema.
            // Placeholder tasks have no accounts; callbacks use a separate transactional probe.
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE [AutomaticTask] NOCHECK CONSTRAINT ALL");
            await context.Database.ExecuteSqlRawAsync("CREATE TABLE ScheduledExecutionProbe (Marker uniqueidentifier NOT NULL)");
            context.AddRange(User(_owner), User(_other), User(_admin));
            context.AppRoles.Add(new EFAppRole { Name = "Admin", Users = new List<AppUser> { context.AppUser.Local.Single(x => x.UserId == _admin) } });
            await context.SaveChangesAsync();
        }

        [OneTimeTearDown]
        public async Task DeleteIsolatedDatabase()
        {
            if (_connection == null) return;
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_connection);
            if (!builder.InitialCatalog.StartsWith("MyFinance_ScheduledExecutionTests_", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to delete a database outside the test namespace.");
            await using var context = Context();
            await context.Database.EnsureDeletedAsync();
        }

        private static AppUser User(Guid id) => new()
            { UserId = id, Username = id.ToString("N"), Name = "Execution test", Password = "not-a-real-password" };

        private DbContextOptions<MyFinanceContext> ContextOptions() =>
            new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options;

        private MyFinanceContext Context() => new LedgerTestContext(ContextOptions());

        private EFScheduledExecutionRepository Repository(MyFinanceContext context) => new(context, ContextOptions(),
            TimeProvider.System, DispatchProxy.Create<IAutomaticTaskRepository, Definitions>(),
            NullLogger<EFScheduledExecutionRepository>.Instance);

        private async Task<Guid> NewTask()
        {
            await using var context = Context();
            var id = Guid.NewGuid();
            context.AutomaticTask.Add(new AutomaticTask
                { AutomaticTaskId = id, UserId = _owner, TaskDescription = "Execution test", Days = "14" });
            await context.SaveChangesAsync();
            return id;
        }

        private async Task<ScheduledTaskOccurrence> Occurrence(Guid taskId, DateTime date)
        {
            await using var context = Context();
            var occurrence = new ScheduledTaskOccurrence
            {
                Id = Guid.NewGuid(), TaskId = taskId, OwnerId = _owner,
                ScheduledLocalDate = date.Date, PostingDate = date.Date,
                DueAtUtc = date.Date.AddHours(6), TimeZoneId = "America/Costa_Rica",
                DefinitionJson = "{\"taskType\":1,\"description\":\"immutable original\"}"
            };
            context.Add(occurrence);
            await context.SaveChangesAsync();
            return occurrence;
        }

        private static Task Probe(MyFinanceContext context, Guid marker) =>
            context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ScheduledExecutionProbe (Marker) VALUES ({marker})");

        private async Task<int> ProbeCount(Guid marker)
        {
            await using var context = Context();
            return await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM ScheduledExecutionProbe WHERE Marker={marker}").SingleAsync();
        }

        [Test]
        public async Task AttemptHistoryIsOwnerTaskScopedOrderedAndPaged()
        {
            var task = await NewTask();
            var otherTask = await NewTask();
            var older = DateTime.UtcNow.AddMinutes(-2);
            var newer = older.AddMinutes(1);
            await using (var context = Context())
            {
                context.AddRange(
                    new ScheduledTaskExecutionAttempt
                    {
                        Id = Guid.NewGuid(), TaskId = task, OwnerId = _owner, ActorId = _owner,
                        ManualRequestId = Guid.NewGuid(), AttemptNumber = 1, PostingDate = new DateTime(2026, 10, 16),
                        StartedAtUtc = older, EndedAtUtc = older.AddSeconds(1), Status = ScheduledTaskAttemptStatus.Failed,
                        FailureStage = "FinancialWork", FailureType = "ServiceException", FailureMessage = "Safe summary"
                    },
                    new ScheduledTaskExecutionAttempt
                    {
                        Id = Guid.NewGuid(), TaskId = task, OwnerId = _owner, ActorId = _owner,
                        ManualRequestId = Guid.NewGuid(), AttemptNumber = 1, PostingDate = new DateTime(2026, 10, 17),
                        StartedAtUtc = newer, EndedAtUtc = newer.AddSeconds(1), Status = ScheduledTaskAttemptStatus.Succeeded
                    },
                    new ScheduledTaskExecutionAttempt
                    {
                        Id = Guid.NewGuid(), TaskId = otherTask, OwnerId = _owner, ActorId = _owner,
                        ManualRequestId = Guid.NewGuid(), AttemptNumber = 1, PostingDate = new DateTime(2026, 10, 17),
                        StartedAtUtc = newer.AddMinutes(1), EndedAtUtc = newer.AddMinutes(1).AddSeconds(1), Status = ScheduledTaskAttemptStatus.Failed
                    },
                    new ScheduledTaskExecutionAttempt
                    {
                        Id = Guid.NewGuid(), TaskId = task, OwnerId = _other, ActorId = _other,
                        ManualRequestId = Guid.NewGuid(), AttemptNumber = 1, PostingDate = new DateTime(2026, 10, 17),
                        StartedAtUtc = newer.AddMinutes(2), EndedAtUtc = newer.AddMinutes(2).AddSeconds(1), Status = ScheduledTaskAttemptStatus.Failed
                    });
                await context.SaveChangesAsync();
            }

            await using (var context = Context())
            {
                var page = await Repository(context).GetAttemptHistoryAsync(
                    new ScheduledTaskAccessScope(_owner, task, ResourceActionNames.View), 0, 1);
                Assert.That(page.Items, Has.Count.EqualTo(1));
                Assert.That(page.Items[0].Status, Is.EqualTo(ScheduledExecutionAttemptStatus.Succeeded));
                Assert.That(page.Items[0].PostingDate, Is.EqualTo(new DateTime(2026, 10, 17)));
                Assert.That(page.HasMore, Is.True);
            }

            await using (var context = Context())
            {
                var page = await Repository(context).GetAttemptHistoryAsync(
                    new ScheduledTaskAccessScope(_owner, task, ResourceActionNames.View), 1, 1);
                Assert.That(page.Items, Has.Count.EqualTo(1));
                Assert.That(page.Items[0].Status, Is.EqualTo(ScheduledExecutionAttemptStatus.Failed));
                Assert.That(page.Items[0].FailureMessage, Is.EqualTo("Safe summary"));
                Assert.That(page.HasMore, Is.False);
            }
        }

        [Test]
        public async Task ConcurrentOwnerAndAdministratorCallsCommitOneEffectAndReturnSameResult()
        {
            var task = await NewTask();
            var occurrence = await Occurrence(task, new DateTime(2020, 1, 14));
            var marker = Guid.NewGuid();
            async Task<ScheduledExecutionResult> Run(Guid actor)
            {
                await using var context = Context();
                return await Repository(context).ExecuteAsync(new(task, actor, occurrence.Id, null, null), async (work, _) =>
                {
                    Assert.That(work.DefinitionJson, Does.Contain("immutable original"));
                    Assert.That(work.PostingDate, Is.EqualTo(occurrence.PostingDate));
                    await Probe(context, marker);
                    await Task.Delay(100);
                });
            }
            var results = await Task.WhenAll(Run(_owner), Run(_admin));
            Assert.That(results.Select(x => x.ExecutionId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(results.Count(x => x.AlreadyCompleted), Is.EqualTo(1));
            Assert.That(await ProbeCount(marker), Is.EqualTo(1));
        }

        [Test]
        public async Task IndependentManualIntentsAndOccurrenceDoNotBlockEachOther()
        {
            var task = await NewTask();
            var occurrence = await Occurrence(task, new DateTime(2020, 1, 14));
            var marker = Guid.NewGuid();
            var manual = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2020, 1, 14));
            async Task<ScheduledExecutionResult> Run(ScheduledExecutionRequest request)
            {
                await using var context = Context();
                return await Repository(context).ExecuteAsync(request, (_, _) => Probe(context, marker));
            }
            var first = await Run(manual);
            var retry = await Run(manual); // Includes recovery after losing the successful response.
            Assert.That(retry.ExecutionId, Is.EqualTo(first.ExecutionId));
            Assert.That(retry.AlreadyCompleted, Is.True);
            await Run(manual with { ManualRequestId = Guid.NewGuid() });
            await Run(new(task, _owner, occurrence.Id, null, null));
            Assert.That(await ProbeCount(marker), Is.EqualTo(3));
        }

        [Test]
        public async Task ReusingManualIdentityForDifferentWorkIsRejected()
        {
            var task = await NewTask();
            var another = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2020, 1, 14));
            await using (var context = Context())
                await Repository(context).ExecuteAsync(request, (_, _) => Task.CompletedTask);
            foreach (var conflict in new[] { request with { TaskId = another }, request with { ManualPostingDate = request.ManualPostingDate.Value.AddDays(1) } })
            {
                await using var context = Context();
                var error = Assert.ThrowsAsync<ServiceException>(() => Repository(context).ExecuteAsync(conflict, (_, _) => Task.CompletedTask));
                Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            }
        }

        [Test]
        public async Task CallbackFailureRollsBackEffectsAndAllowsRetry()
        {
            var task = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2020, 1, 14));
            var marker = Guid.NewGuid();
            await using (var context = Context())
            {
                Assert.ThrowsAsync<InvalidOperationException>(() => Repository(context).ExecuteAsync(request, async (_, _) =>
                {
                    await Probe(context, marker);
                    throw new InvalidOperationException("Injected financial failure");
                }));
            }
            Assert.That(await ProbeCount(marker), Is.Zero);
            await using (var context = Context())
            {
                Assert.That(await context.Set<ScheduledTaskCompletion>().AnyAsync(x => x.ManualRequestId == request.ManualRequestId), Is.False);
                await Repository(context).ExecuteAsync(request, (_, _) => Probe(context, marker));
            }
            Assert.That(await ProbeCount(marker), Is.EqualTo(1));
        }

        [Test]
        public async Task FailedAttemptIsDurableAndRetryGetsANewAttemptNumber()
        {
            var task = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2026, 1, 14));
            await using (var context = Context())
            {
                Assert.ThrowsAsync<ServiceException>(() => Repository(context).ExecuteAsync(request,
                    (_, _) => throw new ServiceException("The task definition is invalid.", HttpStatusCode.Conflict)));
            }

            await using (var context = Context())
            {
                var failed = await context.Set<ScheduledTaskExecutionAttempt>()
                    .SingleAsync(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId);
                Assert.That(failed.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Failed));
                Assert.That(failed.AttemptNumber, Is.EqualTo(1));
                Assert.That(failed.FailureStage, Is.EqualTo("FinancialWork"));
                Assert.That(failed.FailureType, Is.EqualTo(nameof(ServiceException)));
                Assert.That(failed.FailureMessage, Is.EqualTo("The task definition is invalid."));
                Assert.That(failed.EndedAtUtc, Is.Not.Null);
            }

            await using (var context = Context())
            {
                await Repository(context).ExecuteAsync(request, (_, _) => Task.CompletedTask);
                var attempts = await context.Set<ScheduledTaskExecutionAttempt>()
                    .Where(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId)
                    .OrderBy(x => x.AttemptNumber).ToListAsync();
                Assert.That(attempts.Select(x => x.Status), Is.EqualTo(new[]
                    { ScheduledTaskAttemptStatus.Failed, ScheduledTaskAttemptStatus.Succeeded }));
                Assert.That(attempts.Select(x => x.AttemptNumber), Is.EqualTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public async Task RetryMarksPriorUnfinishedAttemptAsInterrupted()
        {
            var task = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2026, 1, 14));
            var abandonedAttemptId = Guid.NewGuid();
            await using (var context = Context())
            {
                context.Add(new ScheduledTaskExecutionAttempt
                {
                    Id = abandonedAttemptId,
                    TaskId = task,
                    OwnerId = _owner,
                    ActorId = _owner,
                    ManualRequestId = request.ManualRequestId,
                    AttemptNumber = 1,
                    PostingDate = request.ManualPostingDate.Value,
                    StartedAtUtc = new DateTime(2026, 1, 14, 12, 0, 0, DateTimeKind.Utc),
                    Status = ScheduledTaskAttemptStatus.InProgress
                });
                await context.SaveChangesAsync();
            }

            await using (var context = Context())
                await Repository(context).ExecuteAsync(request, (_, _) => Task.CompletedTask);

            await using (var context = Context())
            {
                var attempts = await context.Set<ScheduledTaskExecutionAttempt>()
                    .Where(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId)
                    .OrderBy(x => x.AttemptNumber).ToListAsync();
                Assert.That(attempts.Select(x => x.Id), Does.Contain(abandonedAttemptId));
                Assert.That(attempts.Select(x => x.Status), Is.EqualTo(new[]
                    { ScheduledTaskAttemptStatus.Interrupted, ScheduledTaskAttemptStatus.Succeeded }));
                Assert.That(attempts[0].FailureType, Is.EqualTo("ProcessInterrupted"));
                Assert.That(attempts.All(x => x.EndedAtUtc.HasValue), Is.True);
            }
        }

        [Test]
        public async Task ExistingCompletionReconcilesAnUnfinishedAttemptWithoutReexecuting()
        {
            var task = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2026, 1, 14));
            var completedAt = new DateTime(2026, 1, 14, 13, 0, 0, DateTimeKind.Utc);
            var completionId = Guid.NewGuid();
            await using (var context = Context())
            {
                context.AddRange(
                    new ScheduledTaskCompletion
                    {
                        Id = completionId,
                        TaskId = task,
                        ActorId = _owner,
                        ManualRequestId = request.ManualRequestId,
                        PostingDate = request.ManualPostingDate.Value,
                        CompletedAtUtc = completedAt
                    },
                    new ScheduledTaskExecutionAttempt
                    {
                        Id = Guid.NewGuid(),
                        TaskId = task,
                        OwnerId = _owner,
                        ActorId = _owner,
                        ManualRequestId = request.ManualRequestId,
                        AttemptNumber = 1,
                        PostingDate = request.ManualPostingDate.Value,
                        StartedAtUtc = completedAt.AddMinutes(-1),
                        Status = ScheduledTaskAttemptStatus.InProgress
                    });
                await context.SaveChangesAsync();
            }

            await using (var context = Context())
            {
                var result = await Repository(context).ExecuteAsync(request,
                    (_, _) => throw new AssertionException("Committed work was executed again"));
                Assert.That(result.ExecutionId, Is.EqualTo(completionId));
                Assert.That(result.AlreadyCompleted, Is.True);
            }

            await using (var context = Context())
            {
                var attempt = await context.Set<ScheduledTaskExecutionAttempt>()
                    .SingleAsync(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId);
                Assert.That(attempt.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Succeeded));
                Assert.That(attempt.EndedAtUtc, Is.EqualTo(completedAt));
            }
        }

        [Test]
        public async Task CompletionWriteFailureRollsBackPreviouslyWrittenEffects()
        {
            var task = await NewTask();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2020, 1, 14));
            var marker = Guid.NewGuid();
            await using (var context = new CompletionFailureContext(new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options))
                Assert.ThrowsAsync<InvalidOperationException>(() => Repository(context).ExecuteAsync(request, (_, _) => Probe(context, marker)));
            Assert.That(await ProbeCount(marker), Is.Zero);
            await using (var context = Context())
            {
                var failed = await context.Set<ScheduledTaskExecutionAttempt>()
                    .SingleAsync(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId);
                Assert.That(failed.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Failed));
                Assert.That(failed.FailureStage, Is.EqualTo("PersistCompletion"));
                Assert.That(await context.Set<ScheduledTaskCompletion>().AnyAsync(x => x.ManualRequestId == request.ManualRequestId), Is.False);
            }
            await using (var context = Context())
                await Repository(context).ExecuteAsync(request, (_, _) => Probe(context, marker));
            Assert.That(await ProbeCount(marker), Is.EqualTo(1));
            await using (var context = Context())
            {
                var attempts = await context.Set<ScheduledTaskExecutionAttempt>()
                    .Where(x => x.TaskId == task && x.ManualRequestId == request.ManualRequestId)
                    .OrderBy(x => x.AttemptNumber).ToListAsync();
                Assert.That(attempts.Select(x => x.Status), Is.EqualTo(new[]
                    { ScheduledTaskAttemptStatus.Failed, ScheduledTaskAttemptStatus.Succeeded }));
            }
        }

        [Test]
        public async Task UnrelatedUserCannotExecuteAnOccurrenceOrManualExtra()
        {
            var task = await NewTask();
            var occurrence = await Occurrence(task, new DateTime(2020, 1, 14));
            foreach (var request in new[]
            {
                new ScheduledExecutionRequest(task, _other, occurrence.Id, null, null),
                new ScheduledExecutionRequest(task, _other, null, Guid.NewGuid(), DateTime.Today),
                new ScheduledExecutionRequest(task, _admin, null, Guid.NewGuid(), DateTime.Today)
            })
            {
                await using var context = Context();
                var error = Assert.ThrowsAsync<ServiceException>(() => Repository(context).ExecuteAsync(request,
                    (_, _) => throw new AssertionException("Unauthorized callback reached")));
                Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            }
        }

        [Test]
        public async Task FutureUnknownOrNewerOccurrenceIsRejectedWithoutEffects()
        {
            var task = await NewTask();
            await Occurrence(task, new DateTime(2020, 1, 14));
            var newer = await Occurrence(task, new DateTime(2020, 2, 14));
            var future = await Occurrence(task, new DateTime(2099, 1, 14));
            foreach (var id in new[] { Guid.NewGuid(), newer.Id, future.Id })
            {
                await using var context = Context();
                Assert.ThrowsAsync<ServiceException>(() => Repository(context).ExecuteAsync(new(task, _owner, id, null, null),
                    (_, _) => throw new AssertionException("Invalid occurrence callback reached")));
            }
        }

        [Test]
        public async Task SqlUniquenessPreventsDuplicateOccurrences()
        {
            var task = await NewTask();
            var first = await Occurrence(task, new DateTime(2020, 1, 14));
            await using var context = Context();
            first.Id = Guid.NewGuid();
            context.Add(first);
            Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        [Test]
        public async Task JoinedTransactionDoesNotCommitItsCallersWork()
        {
            var marker = Guid.NewGuid();
            await using (var context = Context())
            {
                var work = UnitOfWork(context);
                await using (var outer = await context.Database.BeginTransactionAsync())
                {
                    await work.ExecuteInTransactionAsync(() => Probe(context, marker));
                    Assert.That(context.Database.CurrentTransaction, Is.SameAs(outer));
                    await outer.RollbackAsync();
                }
            }
            Assert.That(await ProbeCount(marker), Is.Zero);
        }

        [Test]
        public async Task StandaloneTransactionCommitsSuccessAndRollsBackFailure()
        {
            var success = Guid.NewGuid(); var failed = Guid.NewGuid();
            await using (var context = Context())
            {
                var work = UnitOfWork(context);
                await work.ExecuteInTransactionAsync(() => Probe(context, success));
                Assert.ThrowsAsync<InvalidOperationException>(() => work.ExecuteInTransactionAsync(async () =>
                {
                    await Probe(context, failed);
                    throw new InvalidOperationException("Injected failure");
                }));
                Assert.That(context.Database.CurrentTransaction, Is.Null);
            }
            Assert.That(await ProbeCount(success), Is.EqualTo(1));
            Assert.That(await ProbeCount(failed), Is.Zero);
        }

        private static EFUnityOfWork UnitOfWork(MyFinanceContext context) => new(context,
            null, null, null, null, null, null, null, null, null, null, null);

        [TestCase(false)]
        [TestCase(true)]
        public async Task CompletionUniqueIndexesRejectDuplicateRowsWithoutRepositoryLocks(bool scheduled)
        {
            var task = await NewTask();
            var occurrence = scheduled ? await Occurrence(task, new DateTime(2020, 1, 14)) : null;
            var requestId = scheduled ? (Guid?)null : Guid.NewGuid();
            ScheduledTaskCompletion Row() => new()
            {
                Id = Guid.NewGuid(), TaskId = task, ActorId = _owner, OccurrenceId = occurrence?.Id,
                ManualRequestId = requestId, PostingDate = new DateTime(2020, 1, 14), CompletedAtUtc = DateTime.UtcNow
            };
            await using var context = Context();
            context.Add(Row());
            await context.SaveChangesAsync();
            context.Add(Row());
            Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        [Test]
        public async Task ConcurrentManualRetriesExecuteOnce()
        {
            var task = await NewTask();
            var marker = Guid.NewGuid();
            var request = new ScheduledExecutionRequest(task, _owner, null, Guid.NewGuid(), new DateTime(2020, 1, 14));
            async Task<ScheduledExecutionResult> Run()
            {
                await using var context = Context();
                return await Repository(context).ExecuteAsync(request, async (_, _) =>
                {
                    await Probe(context, marker);
                    await Task.Delay(100);
                });
            }
            var results = await Task.WhenAll(Run(), Run());
            Assert.That(results.Select(x => x.ExecutionId).Distinct().Count(), Is.EqualTo(1));
            Assert.That(await ProbeCount(marker), Is.EqualTo(1));
        }

        public class Definitions : DispatchProxy
        {
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name != nameof(IAutomaticTaskRepository.GetScheduledByTaskIdAsync))
                    throw new NotSupportedException(method.Name);
                return Task.FromResult<IReadOnlyCollection<BaseScheduledTaskVm>>(new[]
                    { new BasicScheduledTaskVm { Id = Guid.Parse((string)args[0]), Description = "Manual snapshot" } });
            }
        }

        private class LedgerTestContext(DbContextOptions<MyFinanceContext> options) : MyFinanceContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // The legacy model declares both PK and a second index clustered on
                // TransferRecord. Only the disposable test schema needs this correction.
                modelBuilder.Entity<TransferRecord>().Metadata.FindPrimaryKey().SetIsClustered(false);
            }
        }

        private sealed class CompletionFailureContext(DbContextOptions<MyFinanceContext> options) : LedgerTestContext(options)
        {
            public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
                throw new InvalidOperationException("Injected failure writing the completion ledger");
        }
    }
}
