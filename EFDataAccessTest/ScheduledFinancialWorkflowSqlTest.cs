using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using EFDataAccess;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using NUnit.Framework;
using Serilog;

namespace EFDataAccessTest
{
    [TestFixture]
    [Category("LocalSql")]
    [NonParallelizable]
    public sealed partial class ScheduledFinancialWorkflowSqlTest
    {
        private const string ConnectionEnvironmentVariable = "MYFINANCE_SCHEDULED_WORKFLOW_TEST_CONNECTION";
        private const string AllowedDatabaseName = "MYFNDB-2026-10-18-00-21";
        private string _connection;

        [OneTimeSetUp]
        public void ConfigureLocalCopy()
        {
            _connection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(_connection))
                Assert.Ignore($"Set {ConnectionEnvironmentVariable} to the approved local database copy to run financial workflow tests.");

            var builder = new SqlConnectionStringBuilder(_connection);
            if (!builder.DataSource.Contains("(localdb)\\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(builder.InitialCatalog, AllowedDatabaseName, StringComparison.Ordinal))
                throw new InvalidOperationException("Scheduled financial workflow tests only run against the approved LocalDB copy.");

            _connection = builder.ConnectionString;
        }

        [Test]
        public async Task ManualSpendAndTransferCommitOnceAcrossRetries()
        {
            var marker = "Scheduled workflow test " + Guid.NewGuid().ToString("N");
            var taskIds = new List<Guid>();
            try
            {
                var options = Options();
                await using var context = new MyFinanceContext(options);
                var posting = await FindPostingReferencesAsync(context);
                if (posting == null)
                    Assert.Ignore("The local copy has no owner with overlapping account periods, same-currency accounts, and an available category.");

                var execution = CreateExecutionService(context);
                var spendTask = await CreateFinancialTaskAsync(context, posting, false, marker + " basic");
                taskIds.Add(spendTask);
                await AssertSingleSuccessfulExecutionAsync(context, execution, posting, spendTask, marker + " basic", 1);

                var transferTask = await CreateFinancialTaskAsync(context, posting, true, marker + " transfer");
                taskIds.Add(transferTask);
                await AssertSingleSuccessfulExecutionAsync(context, execution, posting, transferTask,
                    marker + " transfer", 2);
            }
            finally
            {
                await CleanupAsync(marker, taskIds);
            }
        }

        private IScheduledTaskExecutionService CreateExecutionService(MyFinanceContext context, TimeProvider clock = null)
        {
            var exchange = DispatchProxy.Create<ITrxExchangeService, IdentityExchange>();
            var accountRepository = new EFAccountRepository(context, NullLogger<EFAccountRepository>.Instance);
            var spendRepository = new EFSpendsRepository(context, exchange, NullLogger<EFSpendsRepository>.Instance);
            var transferRepository = new EFTransferRepository(context, NullLogger<EFTransferRepository>.Instance);
            var appTransferRepository = new EFAppTransferRepository(context);
            var spendTypeRepository = new EFSpendTypeRepository(context);
            var unitOfWork = new EFUnityOfWork(context, null, null, null, spendRepository, null,
                accountRepository, appTransferRepository, spendTypeRepository, transferRepository, null, null);
            var appTransactions = new AppTransactionsSubService(unitOfWork, exchange);
            var spends = new SpendsService(unitOfWork, appTransactions, exchange, NullLogger<SpendsService>.Instance);
            var transfers = new TransferService(spends, exchange, spendTypeRepository, accountRepository,
                transferRepository, spendRepository, appTransactions, unitOfWork);
            var definitions = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            var financialExecutor = new ScheduledTasksService(definitions, accountRepository, spends, transfers,
                Log.Logger, executionOptions: new ScheduledExecutionOptions { HistoryEnabled = false });
            var executionOptions = new ScheduledExecutionOptions { Enabled = true, HistoryEnabled = true };
            var repository = new EFScheduledExecutionRepository(context, Options(), clock ?? TimeProvider.System,
                definitions, NullLogger<EFScheduledExecutionRepository>.Instance);
            return new ScheduledTaskExecutionService(repository, financialExecutor, accountRepository, executionOptions);
        }

        private static Task<Guid> CreateFinancialTaskAsync(MyFinanceContext context, PostingReferences posting,
            bool transfer, string description, bool daily = false)
        {
            ClientScheduledTask definition = transfer
                ? new ClientScheduledTask.Transfer { ToAccountId = posting.DestinationAccountId }
                : new ClientScheduledTask.Basic { IsSpendTrx = true };
            definition.AccountId = posting.SourceAccountId;
            definition.SpendTypeId = posting.CategoryId;
            definition.CurrencyId = posting.CurrencyId;
            definition.Amount = 1;
            definition.Description = description;
            definition.FrequencyType = daily ? ScheduledTaskFrequencyType.Weekly : ScheduledTaskFrequencyType.Manual;
            definition.Days = daily ? Enumerable.Range(0, 7).ToArray() : Array.Empty<int>();
            var definitions = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            return definitions.CreateAsync(new ScheduledTaskCreateScope(posting.OwnerId), definition);
        }

        private async Task AssertSingleSuccessfulExecutionAsync(MyFinanceContext context,
            IScheduledTaskExecutionService execution, PostingReferences posting, Guid taskId,
            string taskDescription, int expectedSpendCount)
        {
            var request = new ScheduledExecutionRequest(taskId, posting.OwnerId, null,
                Guid.NewGuid(), posting.PostingDate);
            var scope = new ScheduledTaskExecutionScope(posting.OwnerId, posting.OwnerId, taskId);
            var first = await execution.ExecuteAsync(request, scope);
            var retry = await execution.ExecuteAsync(request, scope);

            Assert.That(first.AlreadyCompleted, Is.False);
            Assert.That(retry.AlreadyCompleted, Is.True);
            Assert.That(retry.ExecutionId, Is.EqualTo(first.ExecutionId));

            context.ChangeTracker.Clear();
            await AssertCommittedFinancialEffectsAsync(context, taskId, taskDescription, expectedSpendCount);
            var attempt = await context.Set<ScheduledTaskExecutionAttempt>().SingleAsync(x => x.TaskId == taskId);
            Assert.That(attempt.Status, Is.EqualTo(ScheduledTaskAttemptStatus.Succeeded));
            Assert.That(attempt.AttemptNumber, Is.EqualTo(1));
        }

        private static async Task AssertCommittedFinancialEffectsAsync(MyFinanceContext context, Guid taskId,
            string taskDescription, int expectedSpendCount)
        {
            var spends = await context.Set<EFDataAccess.Models.Spend>().AsNoTracking()
                .Where(x => x.Description.Contains(taskDescription))
                .ToListAsync();
            Assert.That(spends, Has.Count.EqualTo(expectedSpendCount));
            Assert.That(await context.ExecutedTask.CountAsync(x => x.AutomaticTaskId == taskId), Is.EqualTo(1));
            Assert.That(await context.Set<ScheduledTaskCompletion>().CountAsync(x => x.TaskId == taskId), Is.EqualTo(1));
            if (expectedSpendCount == 2)
            {
                var spendIds = spends.Select(x => x.SpendId).ToArray();
                Assert.That(await context.Set<EFAppTransfer>().CountAsync(x =>
                    spendIds.Contains(x.SourceAppTrxId) && spendIds.Contains(x.DestinationAppTrxId)), Is.EqualTo(1));
            }
        }

        private async Task<PostingReferences> FindPostingReferencesAsync(MyFinanceContext context)
        {
            var periods = await context.AccountPeriod.AsNoTracking()
                .Where(x => x.AccountId.HasValue && x.InitialDate.HasValue && x.EndDate.HasValue &&
                    x.Account.UserId.HasValue && x.Account.CurrencyId.HasValue)
                .Select(x => new
                {
                    OwnerId = x.Account.UserId.Value,
                    AccountId = x.AccountId.Value,
                    CurrencyId = x.Account.CurrencyId.Value,
                    InitialDate = x.InitialDate.Value,
                    EndDate = x.EndDate.Value
                })
                .ToListAsync();

            foreach (var group in periods.GroupBy(x => new { x.OwnerId, x.CurrencyId }))
            {
                var categoryId = await context.UserSpendType.AsNoTracking()
                    .Where(x => x.UserId == group.Key.OwnerId)
                    .Select(x => (int?)x.SpendTypeId)
                    .FirstOrDefaultAsync();
                if (!categoryId.HasValue)
                    continue;

                var accountPeriods = group.ToArray();
                for (var sourceIndex = 0; sourceIndex < accountPeriods.Length; sourceIndex++)
                {
                    for (var destinationIndex = sourceIndex + 1; destinationIndex < accountPeriods.Length; destinationIndex++)
                    {
                        var source = accountPeriods[sourceIndex];
                        var destination = accountPeriods[destinationIndex];
                        if (source.AccountId == destination.AccountId)
                            continue;

                        var postingDate = source.InitialDate.Date > destination.InitialDate.Date
                            ? source.InitialDate.Date : destination.InitialDate.Date;
                        var endDate = source.EndDate.Date < destination.EndDate.Date
                            ? source.EndDate.Date : destination.EndDate.Date;
                        if (postingDate < endDate)
                            return new PostingReferences(group.Key.OwnerId, source.AccountId, destination.AccountId,
                                categoryId.Value, group.Key.CurrencyId, postingDate);
                    }
                }
            }

            return null;
        }

        private async Task CleanupAsync(string marker, IReadOnlyCollection<Guid> taskIds)
        {
            if (string.IsNullOrWhiteSpace(_connection))
                return;

            await using var context = new MyFinanceContext(Options());
            await using var transaction = await context.Database.BeginTransactionAsync();
            var spends = context.Set<EFDataAccess.Models.Spend>().Where(x => x.Description.Contains(marker));
            var spendIds = await spends.Select(x => x.SpendId).ToArrayAsync();
            context.Set<EFAppTransfer>().RemoveRange(await context.Set<EFAppTransfer>()
                .Where(x => spendIds.Contains(x.SourceAppTrxId) || spendIds.Contains(x.DestinationAppTrxId)).ToListAsync());
            context.Set<SpendOnPeriod>().RemoveRange(await context.Set<SpendOnPeriod>()
                .Where(x => spendIds.Contains(x.SpendId)).ToListAsync());
            context.Set<EFDataAccess.Models.Spend>().RemoveRange(await spends.ToListAsync());

            if (taskIds.Count > 0)
            {
                context.ExecutedTask.RemoveRange(await context.ExecutedTask.Where(x => taskIds.Contains(x.AutomaticTaskId)).ToListAsync());
                context.Set<ScheduledTaskExecutionAttempt>().RemoveRange(await context.Set<ScheduledTaskExecutionAttempt>()
                    .Where(x => taskIds.Contains(x.TaskId)).ToListAsync());
                context.Set<ScheduledTaskCompletion>().RemoveRange(await context.Set<ScheduledTaskCompletion>()
                    .Where(x => taskIds.Contains(x.TaskId)).ToListAsync());
                context.Set<ScheduledTaskOccurrence>().RemoveRange(await context.Set<ScheduledTaskOccurrence>()
                    .Where(x => taskIds.Contains(x.TaskId)).ToListAsync());
                context.Set<ScheduledTaskRevision>().RemoveRange(await context.Set<ScheduledTaskRevision>()
                    .Where(x => taskIds.Contains(x.TaskId)).ToListAsync());
                context.Set<ScheduledTaskSchedule>().RemoveRange(await context.Set<ScheduledTaskSchedule>()
                    .Where(x => taskIds.Contains(x.TaskId)).ToListAsync());
                context.SpInTrxDef.RemoveRange(await context.SpInTrxDef.Where(x => taskIds.Contains(x.SpInTrxDefId)).ToListAsync());
                context.TransferTrxDef.RemoveRange(await context.TransferTrxDef.Where(x => taskIds.Contains(x.TransferTrxDefId)).ToListAsync());
                context.AutomaticTask.RemoveRange(await context.AutomaticTask.Where(x => taskIds.Contains(x.AutomaticTaskId)).ToListAsync());
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            context.ChangeTracker.Clear();
            if (await context.Set<EFDataAccess.Models.Spend>().AsNoTracking().AnyAsync(x => x.Description.Contains(marker)) ||
                await context.AutomaticTask.AsNoTracking().AnyAsync(x => taskIds.Contains(x.AutomaticTaskId)) ||
                await context.ExecutedTask.AsNoTracking().AnyAsync(x => taskIds.Contains(x.AutomaticTaskId)) ||
                await context.Set<ScheduledTaskExecutionAttempt>().AsNoTracking().AnyAsync(x => taskIds.Contains(x.TaskId)) ||
                await context.Set<ScheduledTaskCompletion>().AsNoTracking().AnyAsync(x => taskIds.Contains(x.TaskId)) ||
                await context.Set<ScheduledTaskOccurrence>().AsNoTracking().AnyAsync(x => taskIds.Contains(x.TaskId)) ||
                await context.Set<ScheduledTaskRevision>().AsNoTracking().AnyAsync(x => taskIds.Contains(x.TaskId)) ||
                await context.Set<ScheduledTaskSchedule>().AsNoTracking().AnyAsync(x => taskIds.Contains(x.TaskId)))
                throw new InvalidOperationException("Scheduled workflow test cleanup left temporary rows in the local copy.");
        }

        private DbContextOptions<MyFinanceContext> Options() =>
            new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(_connection).Options;

        private sealed record PostingReferences(Guid OwnerId, int SourceAccountId, int DestinationAccountId,
            int CategoryId, int CurrencyId, DateTime PostingDate);

        private class IdentityExchange : DispatchProxy
        {
            protected override object Invoke(MethodInfo method, object[] args)
            {
                if (method.Name == nameof(ITrxExchangeService.ConvertTrxCurrencyAsync) &&
                    args[0] is IReadOnlyCollection<SpendCurrencyConvertibleItem> items)
                {
                    var converted = items.Select(item => new SpendCurrencyConvertibleResult(item.Guid,
                        GetAccountData(item.SpendCurrencyConvertible))).ToArray();
                    return Task.FromResult<IEnumerable<SpendCurrencyConvertibleResult>>(converted);
                }

                if (method.Name == nameof(ITrxExchangeService.ConvertTrxCurrencyAsync) &&
                    args[0] is ISpendCurrencyConvertible convertible)
                    return Task.FromResult<IEnumerable<AddSpendAccountDbValues>>(GetAccountData(convertible));

                if (method.Name == nameof(ITrxExchangeService.GetExchangeRateResultAsync))
                    return Task.FromResult(ExchangeRateResult.CreateDefaultExchangeRateResult((int)args[0]));

                throw new NotSupportedException(method.Name);
            }

            private static IReadOnlyCollection<AddSpendAccountDbValues> GetAccountData(ISpendCurrencyConvertible item)
            {
                var accounts = new List<ClientAddSpendAccount> { item.OriginalAccountData };
                accounts.AddRange(item.IncludedAccounts);
                return accounts.Select(x => new AddSpendAccountDbValues
                {
                    AccountId = x.AccountId,
                    CurrencyConverterMethodId = x.ConvertionMethodId,
                    Numerator = 1,
                    Denominator = 1,
                    IsOriginal = x.AccountId == item.OriginalAccountData.AccountId
                }).ToArray();
            }
        }
    }
}
