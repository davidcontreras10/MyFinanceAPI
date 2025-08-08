using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.GptClassifiedExpenseCache;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IGptClassifiedExpensesCacheRepository
	{
		Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetByIds(IEnumerable<IGptCacheKey> gptCacheKeys);
		Task UpsertMultipleAsync(IEnumerable<InGptClassifiedExpenseCache> inputItems);
		Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetAllItems();
	}
}
