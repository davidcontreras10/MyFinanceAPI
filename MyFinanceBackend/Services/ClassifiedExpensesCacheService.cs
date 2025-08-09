using MyFinanceBackend.Data;
using MyFinanceModel.GptClassifiedExpenseCache;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IClassifiedExpensesCacheService
	{
		Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetItems();
	}

	public class ClassifiedExpensesCacheService(IGptClassifiedExpensesCacheRepository classifiedExpensesCacheRepository) : IClassifiedExpensesCacheService
	{
		public async Task<IReadOnlyCollection<OutGptClassifiedExpenseCache>> GetItems()
		{
			return await classifiedExpensesCacheRepository.GetAllItems();
		}
	}
}
