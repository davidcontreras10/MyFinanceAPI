using MongoDB.Driver;
using MongoDB.Models;
using MyFinanceBackend.Data;
using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.GptClassifiedExpenseCache;

namespace MongoDB.Repositories
{
	public class GptClassifiedExpensesCacheRepository : MongoRepositoryBase<GptClassifiedExpenseCacheItem, string>, IGptClassifiedExpensesCacheRepository
	{
		public GptClassifiedExpensesCacheRepository(IMongoDatabase database) : base(database, "gptClassifiedExpenses")
		{
		}

		public async Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetAllItems()
		{
			var items = await Collection.Find(FilterDefinition<GptClassifiedExpenseCacheItem>.Empty).ToListAsync();
			return items.Select(ToOutGptClassifiedExpenseCache).ToList();
		}

		public async Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetByIds(IEnumerable<IGptCacheKey> gptCacheKeys)
		{
			if (gptCacheKeys == null || !gptCacheKeys.Any())
			{
				return [];
			}
			var filter = Builders<GptClassifiedExpenseCacheItem>.Filter.In(
				x => x.Id,
				gptCacheKeys.Select(GptClassifiedExpenseCacheItem.GenerateCacheKey)
			);
			var items = await Collection.Find(filter).ToListAsync();
			return items.Select(ToOutGptClassifiedExpenseCache).ToList();
		}

		public async Task UpsertMultipleAsync(IEnumerable<InGptClassifiedExpenseCache> inputItems)
		{
			if (inputItems == null || !inputItems.Any())
			{
				return; // No items to upsert
			}

			var items = inputItems.Select(ToGptClassifiedExpenseCacheItem).ToList();
			var bulkOps = items.Select(item => new ReplaceOneModel<GptClassifiedExpenseCacheItem>(
				Builders<GptClassifiedExpenseCacheItem>.Filter.Eq(x => x.Id, item.Id),
				item)
			{
				IsUpsert = true
			}).ToList();
			if (bulkOps.Any())
			{
				await Collection.BulkWriteAsync(bulkOps);
			}
		}

		private static GptClassifiedExpenseCacheItem ToGptClassifiedExpenseCacheItem(InGptClassifiedExpenseCache item)
		{
			return new GptClassifiedExpenseCacheItem
			{
				Description = item.Description,
				Amount = item.Amount,
				Currency = item.Currency,
				Category = item.Category,
				CategoryId = item.CategoryId,
				CategoryConfidence = item.CategoryConfidence,
				AccountId = item.AccountId,
				AccountName = item.AccountName,
				AccountConfidence = item.AccountConfidence,
				LastUpdated = DateTime.UtcNow
			};
		}

		private static OutGptClassifiedExpenseCache ToOutGptClassifiedExpenseCache(GptClassifiedExpenseCacheItem item)
		{
			return new OutGptClassifiedExpenseCache
			{
				Description = item.Description,
				Amount = item.Amount,
				Currency = item.Currency,
				Category = item.Category,
				CategoryId = item.CategoryId,
				CategoryConfidence = item.CategoryConfidence,
				AccountId = item.AccountId,
				AccountName = item.AccountName,
				AccountConfidence = item.AccountConfidence,
				LastUpdated = item.LastUpdated
			};
		}
	}
}
