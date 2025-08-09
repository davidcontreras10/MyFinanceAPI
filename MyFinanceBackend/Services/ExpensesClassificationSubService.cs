using MyFinanceBackend.Data;
using MyFinanceBackend.Utils;
using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.GptClassifiedExpenseCache;
using MyFinanceModel.Mappers;
using MyFinanceModel.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class ExpensesClassificationSubService(
		IGptClassifiedExpensesCacheRepository classifiedExpensesCacheRepository,
		IUnitOfWork unitOfWork,
		IBankTrxCategorizationRepository bankTrxCategorizationRepository) : IExpensesClassificationSubService
	{
		private const int LastHistoricalMonths = 3;

		public async Task<IReadOnlyCollection<ClassifiedBankTrx>> GetClassifiedBankTransactionsAsync(string userId)
		{
			return await unitOfWork.BankTransactionsRepository.GetClassifiedBankTransactionsAsync(6, userId, null);
		}

		public async Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExistingBankTransactionsAsync(
			IReadOnlyCollection<string> refNumbers,
			int financialEntityId,
			string userId)
		{
			var toClassifyData = await unitOfWork.BankTransactionsRepository.GetToClassifyBankTransactionsAsync(financialEntityId, refNumbers);
			var expensesToClassify = toClassifyData.Select(BankTrxCategorizationMapper.ToExpenseToClassify).ToList();
			var cacheSearchResults = await GetCachedClassifiedExpensesAsync(expensesToClassify);
			if (cacheSearchResults.NotCachedItems.Count == 0)
			{
				return cacheSearchResults.CachedItems;
			}

			expensesToClassify = cacheSearchResults.NotCachedItems;
			var fromLastXMonthsDate = DateTime.UtcNow.AddMonths(LastHistoricalMonths * -1);
			var classifiedData = await unitOfWork.BankTransactionsRepository.GetClassifiedBankTransactionsAsync(financialEntityId, userId, fromLastXMonthsDate);
			var historicalExamples = classifiedData.Select(BankTrxCategorizationMapper.ToInHisotricClassfiedExpense);
			historicalExamples = ExpenseDataCleanup<InHisotricClassfiedExpense>.Clean([.. historicalExamples]);

			var spendTypes = await unitOfWork.SpendTypeRepository.GetSpendTypesAsync(userId, false);
			var categories = spendTypes.Where(x => !string.IsNullOrWhiteSpace(x.SpendTypeName))
				.Select(BankTrxCategorizationMapper.ToGptCategory).ToList();

			var accounts = await unitOfWork.AccountRepository.GetAiClassifiableAccountsAsync(userId);
			var accountDescriptions = accounts.Where(x => !string.IsNullOrWhiteSpace(x.AiClassificationHint) && !string.IsNullOrWhiteSpace(x.AccountName))
				.Select(BankTrxCategorizationMapper.ToGptAccount).ToList();

			var aiClassificationResults = await bankTrxCategorizationRepository.ClassifyExpensesWithGptAsync(
				expensesToClassify,
				categories,
				accountDescriptions,
				[.. historicalExamples]);

			var toCacheItems = aiClassificationResults
				.Select(BankTrxCategorizationMapper.ToInGptClassifiedExpenseCache)
				.ToList();

			toCacheItems.ApplyDescriptionNormalizations();
			await classifiedExpensesCacheRepository.UpsertMultipleAsync(toCacheItems);
			var cachedItemsResult = cacheSearchResults.CachedItems;
			if (cachedItemsResult == null)
			{
				cachedItemsResult = [];
			}
			var allResults = cachedItemsResult.Concat(aiClassificationResults).ToList();
			return allResults;
		}

		private async Task<CacheSearchResults> GetCachedClassifiedExpensesAsync(List<ExpenseToClassify> expenseToClassify)
		{
			if (expenseToClassify == null || expenseToClassify.Count == 0)
			{
				return new CacheSearchResults([], expenseToClassify);
			}

			var cacheKeys = expenseToClassify
				.Select(x => new GptCacheKey(x.Id)
				{
					Description = x.Description,
					Amount = x.Amount,
					Currency = x.Currency
				}).ToList();

			ExpenseDataExtensions.ApplyDescriptionNormalizations(cacheKeys);
			var itemsInCache = await classifiedExpensesCacheRepository.GetByIds(cacheKeys);
			List<ExpenseToClassify> notCachedItems = [];
			List<OutGptClassifiedExpense> cachedItems = [];
			foreach (var item in expenseToClassify)
			{
				var cacheKeyItem = cacheKeys.First(x => x.RefId == item.Id);
				var cachedItem = itemsInCache.FirstOrDefault(x => x.EqualsExt(cacheKeyItem));
				if (cachedItem != null)
				{
					var outPutItem = ToOutGptClassifiedExpenseCache(cachedItem, item);
					cachedItems.Add(outPutItem);
				}
				else
				{
					notCachedItems.Add(item);
				}
			}

			return new CacheSearchResults(cachedItems, notCachedItems);
		}

		private static OutGptClassifiedExpense ToOutGptClassifiedExpenseCache(OutGptClassifiedExpenseCache cachedItem, ExpenseToClassify expenseToClassify)
		{
			return new OutGptClassifiedExpense
			{
				Description = cachedItem.Description,
				Amount = cachedItem.Amount,
				Currency = cachedItem.Currency,
				AccountName = cachedItem.AccountName,
				Id = expenseToClassify.Id,
				AccountConfidence = cachedItem.AccountConfidence,
				Category = cachedItem.Category,
				CategoryConfidence = cachedItem.CategoryConfidence,
				AccountId = cachedItem.AccountId,
				CategoryId = cachedItem.CategoryId
			};
		}

		private record GptCacheKey(string RefId) : IGptCacheKey
		{
			public string Description { get; set; }
			public decimal Amount { get; set; }
			public string Currency { get; set; }
		}

		private record CacheSearchResults(
			IReadOnlyCollection<OutGptClassifiedExpense> CachedItems,
			List<ExpenseToClassify> NotCachedItems
		);
	}
}
