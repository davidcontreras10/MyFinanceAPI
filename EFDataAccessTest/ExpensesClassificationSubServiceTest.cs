using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceBackend.Utils;
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
		public async Task FullyCachedIvaBatchReusesEveryResultWithoutCallingGpt(string taxDescription)
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "purchase"), "MICROSOFT AZURE HOSTING", 15m, "USD"),
				new(new(6, "tax"), taxDescription, 1.95m, "USD")
			];
			var cache = new Cache(rows);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows, cacheOnly: true), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["purchase", "tax"], 6, UserId);
			Assert.That(cache.ReadCalls, Is.EqualTo(1));
			Assert.That(classifier.InputBatches, Is.Empty);
			Assert.That(result.Select(e => e.Id), Is.EquivalentTo(new[] { "purchase", "tax" }));
			Assert.That(result.Single(e => e.Id == "purchase").AccountConfidence, Is.EqualTo("High"));
			Assert.That(result.Single(e => e.Id == "tax").AccountId, Is.EqualTo(4016));
			Assert.That(cache.WrittenItems, Is.Empty);
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
			var service = new ExpensesClassificationSubService(cache, Unit(rows, cacheOnly: true), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["purchase"], 6, UserId);
			Assert.That(result, Has.Count.EqualTo(1));
			Assert.That(cache.ReadCalls, Is.EqualTo(1));
			Assert.That(classifier.Inputs, Is.Null);
			Assert.That(cache.WrittenItems, Is.Empty);
		}

		[Test]
		public async Task CachedIvaWithoutPurchaseReusesPreviousAssignment()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "tax"), "IVA Servicio Digital Transf", 1.95m, "USD")];
			var cache = new Cache(rows);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows, cacheOnly: true), classifier);
			var result = await service.ClassifyExistingBankTransactionsAsync(["tax"], 6, UserId);
			Assert.That(cache.ReadCalls, Is.EqualTo(1));
			Assert.That(classifier.InputBatches, Is.Empty);
			Assert.That(result.Single().AccountId, Is.EqualTo(4016));
			Assert.That(cache.WrittenItems, Is.Empty);
		}

		[TestCase(1, true)]
		[TestCase(3, true)]
		[TestCase(1, false)]
		[TestCase(3, false)]
		public async Task RepeatedMixedBatchCachesEveryTransactionIncludingIva(int taxCount, bool purchasesAlreadyCached)
		{
			var purchases = Enumerable.Range(1, 15).Select(i =>
				new ToClassifyBankTrx(new(6, $"purchase-{i}"), $"MERCHANT {i}", i * 10m, "USD")).ToArray();
			var taxes = Enumerable.Range(1, taxCount).Select(i =>
				new ToClassifyBankTrx(new(6, $"tax-{i}"), "IVA Servicio Digital Transf", i * 1.3m, "USD"));
			var rows = purchases.Concat(taxes).ToArray();
			var references = rows.Select(r => r.BankTrxId.TransactionId).ToArray();
			var cache = new Cache(purchasesAlreadyCached ? purchases : []);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			for (var attempt = 0; attempt < 2; attempt++)
			{
				var result = await service.ClassifyExistingBankTransactionsAsync(references, 6, UserId);
				Assert.That(result.Select(r => r.Id), Is.EquivalentTo(references));
			}
			Assert.That(cache.ReadCalls, Is.EqualTo(2));
			Assert.That(classifier.InputBatches, Has.Count.EqualTo(1));
			var initiallyMissingRows = purchasesAlreadyCached ? rows.Skip(15) : rows;
			Assert.That(classifier.InputBatches.Single().Select(r => r.Id),
				Is.EquivalentTo(initiallyMissingRows.Select(r => r.BankTrxId.TransactionId)));
			Assert.That(cache.WriteBatches, Has.Count.EqualTo(1));
			Assert.That(cache.WrittenItems, Has.Count.EqualTo(purchasesAlreadyCached ? taxCount : rows.Length));
			Assert.That(cache.WrittenItems.Count(r => r.Description == "IVA Servicio Digital Transf"), Is.EqualTo(taxCount));
		}

		[Test]
		public async Task IvaCacheKeyDoesNotDependOnTransactionReferenceOrAccompanyingPurchase()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "new-reference"), "IVA Servicio Digital Transf", 1.95m, "USD")];
			var cache = new Cache([new(new(6, "tax"), "IVA Servicio Digital Transf", 1.95m, "USD")]);
			var classifier = new Classifier();
			var result = await new ExpensesClassificationSubService(cache, Unit(rows, cacheOnly: true), classifier)
				.ClassifyExistingBankTransactionsAsync(["new-reference"], 6, UserId);
			Assert.That(result.Single().Id, Is.EqualTo("new-reference"));
			Assert.That(result.Single().AccountId, Is.EqualTo(4016));
			Assert.That(classifier.InputBatches, Is.Empty);
			Assert.That(cache.WriteBatches, Is.Empty);
		}

		[Test]
		public async Task MixedBatchCachesNewPurchasesAndIvaThenReusesAllResults()
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "cached"), "AZURE", 15m, "USD"),
				new(new(6, "new"), "OPENAI", 20m, "USD"),
				new(new(6, "tax"), "IVA Servicio Digital Transf", 2.6m, "USD")
			];
			var cache = new Cache([rows[0]]);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			for (var attempt = 0; attempt < 2; attempt++)
			{
				var result = await service.ClassifyExistingBankTransactionsAsync(["cached", "new", "tax"], 6, UserId);
				Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { "cached", "new", "tax" }));
			}
			Assert.That(classifier.InputBatches[0].Select(r => r.Id), Is.EqualTo(new[] { "new", "tax" }));
			Assert.That(classifier.InputBatches, Has.Count.EqualTo(1));
			Assert.That(cache.WriteBatches, Has.Count.EqualTo(1));
			Assert.That(cache.WrittenItems.Select(r => r.Description), Is.EquivalentTo(new[] { "OPENAI", "IVA Servicio Digital Transf" }));
		}

		[Test]
		public async Task ForeignAccountCacheEntryIsNotReusedInMixedIvaBatch()
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "purchase"), "AZURE", 15m, "USD"),
				new(new(6, "tax"), "IVA Servicio Digital Transf", 1.95m, "USD")
			];
			var cache = new Cache([rows[0]], accountId: 999);
			var classifier = new Classifier();
			await new ExpensesClassificationSubService(cache, Unit(rows), classifier)
				.ClassifyExistingBankTransactionsAsync(["purchase", "tax"], 6, UserId);
			Assert.That(classifier.Inputs.Select(r => r.Id), Is.EqualTo(new[] { "purchase", "tax" }));
		}

		[Test]
		public async Task RepeatedNonTaxRequestUsesTheResultWrittenByTheFirstRequest()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "00123"), "MICROSOFT#G123456, MSBILL.INFO, USA", 15.53m, "USD")];
			var cache = new Cache([]);
			var classifier = new Classifier();
			var service = new ExpensesClassificationSubService(cache, Unit(rows), classifier);
			var first = await service.ClassifyExistingBankTransactionsAsync(["00123"], 6, UserId);
			var second = await service.ClassifyExistingBankTransactionsAsync(["00123"], 6, UserId);
			Assert.That(first.Single().Id, Is.EqualTo("00123"));
			Assert.That(second.Single().Id, Is.EqualTo("00123"));
			Assert.That(second.Single().AccountId, Is.EqualTo(first.Single().AccountId));
			Assert.That(second.Single().CategoryId, Is.EqualTo(first.Single().CategoryId));
			Assert.That(second.Single().Amount, Is.EqualTo(15.53m));
			Assert.That(second.Single().Currency, Is.EqualTo("USD"));
			Assert.That(cache.ReadCalls, Is.EqualTo(2));
			Assert.That(classifier.InputBatches, Has.Count.EqualTo(1));
			Assert.That(cache.WriteBatches, Has.Count.EqualTo(1));
			Assert.That(cache.WrittenItems.Single().Description, Is.EqualTo("MICROSOFT#G<ID>, MSBILL.INFO, USA"));
			Assert.That(rows.Single().Description, Is.EqualTo("MICROSOFT#G123456, MSBILL.INFO, USA"));
		}

		[Test]
		public async Task PartialCacheHitSendsOnlyMissingNonTaxExpensesToClassifier()
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "cached"), "CACHED MERCHANT", 20m, "USD"),
				new(new(6, "missing"), "NEW MERCHANT", 25m, "USD")
			];
			var cache = new Cache([rows[0]]);
			var classifier = new Classifier();
			var result = await new ExpensesClassificationSubService(cache, Unit(rows), classifier)
				.ClassifyExistingBankTransactionsAsync(["cached", "missing"], 6, UserId);
			Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { "cached", "missing" }));
			Assert.That(classifier.Inputs.Single().Id, Is.EqualTo("missing"));
			Assert.That(cache.ReadCalls, Is.EqualTo(1));
			Assert.That(cache.WrittenItems.Single().Description, Is.EqualTo("NEW MERCHANT"));
		}

		[Test]
		public async Task CachedMerchantReusedForMultipleReferencesPreservesEveryTransactionId()
		{
			ToClassifyBankTrx[] rows =
			[
				new(new(6, "0001"), "SAME MERCHANT", 20m, "USD"),
				new(new(6, "0002"), "SAME MERCHANT", 20m, "USD")
			];
			var cache = new Cache([rows[0]]);
			var classifier = new Classifier();
			var result = await new ExpensesClassificationSubService(cache, Unit(rows, cacheOnly: true), classifier)
				.ClassifyExistingBankTransactionsAsync(["0001", "0002"], 6, UserId);
			Assert.That(result.Select(r => r.Id), Is.EquivalentTo(new[] { "0001", "0002" }));
			Assert.That(result.All(r => r.AccountId == 3004 && r.CategoryId == 1
				&& r.AccountName == "General Bac" && r.Category == "Varios"
				&& r.AccountConfidence == "High" && r.CategoryConfidence == "High"), Is.True);
			Assert.That(classifier.InputBatches, Is.Empty);
			Assert.That(cache.WriteBatches, Is.Empty);
		}

		[TestCase(21, "USD")]
		[TestCase(20, "CRC")]
		public async Task DifferentAmountOrCurrencyDoesNotReuseCachedClassification(int amount, string currency)
		{
			ToClassifyBankTrx[] rows = [new(new(6, "request"), "SAME MERCHANT", amount, currency)];
			var cache = new Cache([new(new(6, "previous"), "SAME MERCHANT", 20m, "USD")]);
			var classifier = new Classifier();
			await new ExpensesClassificationSubService(cache, Unit(rows), classifier)
				.ClassifyExistingBankTransactionsAsync(["request"], 6, UserId);
			Assert.That(classifier.Inputs.Single().Id, Is.EqualTo("request"));
			Assert.That(cache.WriteBatches, Has.Count.EqualTo(1));
		}

		[Test]
		public async Task ForeignAccountCacheEntryIsNotReturnedToCurrentUser()
		{
			ToClassifyBankTrx[] rows = [new(new(6, "purchase"), "MERCHANT", 20m, "USD")];
			var cache = new Cache(rows, accountId: 999);
			var classifier = new Classifier();
			var result = await new ExpensesClassificationSubService(cache, Unit(rows), classifier)
				.ClassifyExistingBankTransactionsAsync(["purchase"], 6, UserId);
			Assert.That(classifier.InputBatches, Has.Count.EqualTo(1));
			Assert.That(result.Single().AccountId, Is.EqualTo(3004));
		}

		[Test]
		public async Task EmptyBatchDoesNotReadCacheOrCallClassifier()
		{
			var cache = new Cache([]) { FailOnRead = true };
			var classifier = new Classifier();
			var result = await new ExpensesClassificationSubService(cache, Unit([]), classifier)
				.ClassifyExistingBankTransactionsAsync([], 6, UserId);
			Assert.That(result, Is.Empty);
			Assert.That(cache.ReadCalls, Is.Zero);
			Assert.That(classifier.InputBatches, Is.Empty);
			Assert.That(cache.WriteBatches, Is.Empty);
		}

		private static IUnitOfWork Unit(IReadOnlyCollection<ToClassifyBankTrx> rows, bool cacheOnly = false)
		{
			var bank = Stub<IBankTransactionsRepository>((method, args) => method.Name switch
			{
				nameof(IBankTransactionsRepository.GetToClassifyBankTransactionsAsync) => Task.FromResult(rows),
				nameof(IBankTransactionsRepository.GetClassifiedBankTransactionsAsync) when !cacheOnly => Task.FromResult<IReadOnlyCollection<ClassifiedBankTrx>>([]),
				_ => throw new InvalidOperationException(method.Name)
			});
			var accounts = Stub<IAccountRepository>((method, args) =>
			{
				Assert.That(args[0], Is.EqualTo(UserId));
				return method.Name switch
				{
					nameof(IAccountRepository.GetAiClassifiableAccountsAsync) when !cacheOnly => Task.FromResult<IReadOnlyCollection<AiClassifiableAccount>>([new(3004, "General Bac", "Non-AI services"), new(4016, "Ingresos Ahorros", "AI subscriptions")]),
					nameof(IAccountRepository.GetMatchedAccountIdsByUserIdAsync) => Task.FromResult<IReadOnlyCollection<int>>(
						((IEnumerable<int>)args[1]).Where(id => id is 3004 or 4016).ToArray()),
					_ => throw new InvalidOperationException(method.Name)
				};
			});
			var categories = Stub<ISpendTypeRepository>((method, args) => Task.FromResult<IEnumerable<SpendTypeViewModel>>([new() { SpendTypeId = 1, SpendTypeName = "Varios" }]));
			return Stub<IUnitOfWork>((method, args) => method.Name switch
			{
				"get_BankTransactionsRepository" => bank,
				"get_AccountRepository" => accounts,
				"get_SpendTypeRepository" when !cacheOnly => categories,
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
			public List<IReadOnlyCollection<ExpenseToClassify>> InputBatches { get; } = [];
			public Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExpensesWithGptAsync(List<ExpenseToClassify> inputExpenses, List<Gpt.Category> categories, List<Gpt.Account> accounts, List<InHisotricClassfiedExpense> history)
			{
				Inputs = inputExpenses;
				InputBatches.Add(inputExpenses.ToArray());
				return Task.FromResult<IReadOnlyCollection<OutGptClassifiedExpense>>(inputExpenses.Select(e => new OutGptClassifiedExpense
				{
					Id = e.Id, Description = e.Description, Amount = e.Amount, Currency = e.Currency,
					AccountId = 3004, AccountName = "General Bac", CategoryId = 1, Category = "Varios"
				}).ToList());
			}
		}

		private class Cache(IReadOnlyCollection<ToClassifyBankTrx> rows, int accountId = 3004) : IGptClassifiedExpensesCacheRepository
		{
			private readonly List<OutGptClassifiedExpenseCache> _items = rows.Select(e => new OutGptClassifiedExpenseCache
			{
				Description = ExpenseDataExtensions.NormalizeDescription(e.Description), Amount = e.OriginalAmount, Currency = e.CurrencyCode,
				AccountId = e.BankTrxId.TransactionId == "tax" ? 4016 : accountId,
				AccountName = e.BankTrxId.TransactionId == "tax" ? "Ingresos Ahorros" : "General Bac",
				AccountConfidence = "High", CategoryId = 1, Category = "Varios", CategoryConfidence = "High"
			}).ToList();
			public bool FailOnRead { get; init; }
			public int ReadCalls { get; private set; }
			public List<InGptClassifiedExpenseCache> WrittenItems { get; private set; } = [];
			public List<IReadOnlyCollection<InGptClassifiedExpenseCache>> WriteBatches { get; } = [];
			public Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetByIds(IEnumerable<IGptCacheKey> keys)
			{
				ReadCalls++;
				if (FailOnRead) throw new InvalidOperationException("This batch must not access the cache.");
				var requested = keys.ToArray();
				return Task.FromResult<IReadOnlyCollection<OutGptClassifiedExpenseCache>>(
					_items.Where(item => requested.Any(key => key.EqualsExt(item))).ToArray());
			}
			public Task UpsertMultipleAsync(IEnumerable<InGptClassifiedExpenseCache> items)
			{
				WrittenItems = items.ToList();
				WriteBatches.Add(WrittenItems.ToArray());
				foreach (var item in WrittenItems)
				{
					_items.RemoveAll(existing => existing.Description == item.Description
						&& existing.Amount == item.Amount && existing.Currency == item.Currency);
					_items.Add(new OutGptClassifiedExpenseCache
					{
						Description = item.Description, Amount = item.Amount, Currency = item.Currency,
						AccountId = item.AccountId, AccountName = item.AccountName, AccountConfidence = item.AccountConfidence,
						CategoryId = item.CategoryId, Category = item.Category, CategoryConfidence = item.CategoryConfidence
					});
				}
				return Task.CompletedTask;
			}
			public Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetAllItems() => throw new NotImplementedException();
		}
	}
}
