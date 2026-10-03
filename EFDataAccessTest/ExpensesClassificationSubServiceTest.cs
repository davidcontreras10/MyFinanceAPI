using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.GptClassifiedExpenseCache;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class ExpensesClassificationSubServiceTest
	{
		private const string UserId = "017844b8-a92a-44b0-9faf-e4e7230959b1";

		[TestCase("IVA Servicio Digital Transf")]
		[TestCase(" iva servicio digital transf ")]
		public async Task IvaBatchKeepsCachedPurchaseContextAndDoesNotReuseOrStoreTax(string taxDescription)
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "purchase"), "MICROSOFT AZURE HOSTING", 15m, "USD"),
				new(new(6, "tax"), taxDescription, 1.95m, "USD")
			];
			var cache = new Cache(rows);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["purchase", "tax"], 6, UserId);
			Assert.That(cache.ReadCalls, Is.Zero);
			Assert.That(classifier.Inputs.Select(e => e.Id), Is.EquivalentTo(new[] { "purchase", "tax" }));
			Assert.That(result.Single(e => e.Id == "tax").AccountId, Is.EqualTo(3004));
			Assert.That(cache.WrittenItems, Has.Count.EqualTo(1));
			Assert.That(cache.WrittenItems.Single().Description, Is.EqualTo("MICROSOFT AZURE HOSTING"));
		}

		[Test]
		public async Task ComparisonLoadsOnlyCurrentUserDataOnceAndNeverUsesCache()
		{
			var historyCalls = 0;
			var bank = Stub<IBankTransactionsRepository>((method, args) =>
			{
				Assert.That(method.Name, Is.EqualTo(nameof(IBankTransactionsRepository.GetClassifiedBankTransactionsAsync)));
				Assert.That(args[1], Is.EqualTo(UserId));
				historyCalls++;
				return Task.FromResult<IReadOnlyCollection<ClassifiedBankTrx>>([
					new(20m, "USD", "OPENAI", "Old account", "Varios"),
					new(15m, "USD", "AZURE", "Old account", "Varios")]);
			});
			var normalUnit = Unit([]);
			var unit = Stub<IUnitOfWork>((method, args) => method.Name switch
			{
				"get_BankTransactionsRepository" => bank,
				"get_AccountRepository" => normalUnit.AccountRepository,
				"get_SpendTypeRepository" => normalUnit.SpendTypeRepository,
				_ => throw new InvalidOperationException(method.Name)
			});
			var cache = new Cache([]);
			var service = new ExpensesClassificationSubService(cache, unit, new Classifier());
			var comparison = await service.CompareClassificationModelsAsync(6, 2, UserId);
			Assert.That(historyCalls, Is.EqualTo(1));
			Assert.That(comparison.Inputs.Select(i => i.Description), Is.EqualTo(new[] { "AZURE", "OPENAI" }));
			Assert.That(comparison.Inputs.Select(i => i.Id).Distinct().Count(), Is.EqualTo(2));
			Assert.That(cache.ReadCalls, Is.Zero);
			Assert.That(cache.WrittenItems, Is.Empty);
		}

		[Test]
		public async Task NonTaxCachedPurchaseStillSkipsGpt()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "purchase"), "MICROSOFT AZURE HOSTING", 15m, "USD")];
			var cache = new Cache(rows);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["purchase"], 6, UserId);
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(cache.ReadCalls, Is.EqualTo(1));
			Assert.That(classifier.Inputs, Is.Null);
			Assert.That(cache.WrittenItems, Is.Empty);
		}

		[Test]
		public async Task IvaWithoutPurchaseContextDoesNotReusePreviousTaxAssignment()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "tax"), "IVA Servicio Digital Transf", 1.95m, "USD")];
			var cache = new Cache(rows);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["tax"], 6, UserId);
			Assert.That(cache.ReadCalls, Is.Zero);
			Assert.That(classifier.Inputs, Has.Count.EqualTo(1));
			Assert.That(result.Single().AccountId, Is.EqualTo(3004));
			Assert.That(cache.WrittenItems, Is.Empty);
		}

		private static IUnitOfWork Unit(IReadOnlyCollection<ToClassifyBankTrx> rows)
		{
			var bank = Stub<IBankTransactionsRepository>((method, args) => method.Name switch
			{
				nameof(IBankTransactionsRepository.GetToClassifyBankTransactionsAsync) => Task.FromResult(rows),
				nameof(IBankTransactionsRepository.GetClassifiedBankTransactionsAsync) => Task.FromResult<IReadOnlyCollection<ClassifiedBankTrx>>([]),
				_ => throw new InvalidOperationException(method.Name)
			});
			var accounts = Stub<IAccountRepository>((method, args) =>
			{
				Assert.That(args[0], Is.EqualTo(UserId));
				return method.Name switch
				{
					nameof(IAccountRepository.GetAiClassifiableAccountsAsync) => Task.FromResult<IReadOnlyCollection<AiClassifiableAccount>>([new(3004, "General Bac", "Non-AI services"), new(4016, "Ingresos Ahorros", "AI subscriptions")]),
					nameof(IAccountRepository.GetMatchedAccountIdsByUserIdAsync) => Task.FromResult<IReadOnlyCollection<int>>([3004, 4016]),
					_ => throw new InvalidOperationException(method.Name)
				};
			});
			var categories = Stub<ISpendTypeRepository>((method, args) => Task.FromResult<IEnumerable<SpendTypeViewModel>>([new() { SpendTypeId = 1, SpendTypeName = "Varios" }]));
			return Stub<IUnitOfWork>((method, args) => method.Name switch
			{
				"get_BankTransactionsRepository" => bank,
				"get_AccountRepository" => accounts,
				"get_SpendTypeRepository" => categories,
				_ => throw new InvalidOperationException(method.Name)
			});
		}

		private static T Stub<T>(Func<MethodInfo, object[], object> handler) where T : class
		{
			var proxy = DispatchProxy.Create<T, RepositoryStub>();
			((RepositoryStub)(object)proxy).Handler = handler;
			return proxy;
		}

		public class RepositoryStub : DispatchProxy
		{
			public Func<MethodInfo, object[], object> Handler { get; set; }
			protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args);
		}

		private class Classifier : IBankTrxCategorizationRepository
		{
			public Task<ClassificationComparison> CompareModelsAsync(List<ExpenseToClassify> inputs, List<Gpt.Category> categories,
				List<Gpt.Account> accounts, List<InHisotricClassfiedExpense> history) =>
				Task.FromResult(new ClassificationComparison { Inputs = inputs, Runs = [] });
			public List<ExpenseToClassify> Inputs { get; private set; }
			public Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExpensesWithGptAsync(List<ExpenseToClassify> inputExpenses, List<Gpt.Category> categories, List<Gpt.Account> accounts, List<InHisotricClassfiedExpense> history)
			{
				Inputs = inputExpenses;
				return Task.FromResult<IReadOnlyCollection<OutGptClassifiedExpense>>(inputExpenses.Select(e => new OutGptClassifiedExpense
				{
					Id = e.Id, Description = e.Description, Amount = e.Amount, Currency = e.Currency,
					AccountId = 3004, AccountName = "General Bac", CategoryId = 1, Category = "Varios"
				}).ToList());
			}
		}

		private class Cache(IReadOnlyCollection<ToClassifyBankTrx> rows) : IGptClassifiedExpensesCacheRepository
		{
			public int ReadCalls { get; private set; }
			public List<InGptClassifiedExpenseCache> WrittenItems { get; private set; } = [];
			public Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetByIds(IEnumerable<IGptCacheKey> keys)
			{
				ReadCalls++;
				return Task.FromResult<IReadOnlyCollection<OutGptClassifiedExpenseCache>>(rows.Select(e => new OutGptClassifiedExpenseCache
				{
					Description = e.Description, Amount = e.OriginalAmount, Currency = e.CurrencyCode,
					AccountId = e.BankTrxId.TransactionId == "tax" ? 4016 : 3004
				}).ToList());
			}
			public Task UpsertMultipleAsync(IEnumerable<InGptClassifiedExpenseCache> items)
			{
				WrittenItems = items.ToList();
				return Task.CompletedTask;
			}
			public Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetAllItems() => throw new NotImplementedException();
		}
	}
}
