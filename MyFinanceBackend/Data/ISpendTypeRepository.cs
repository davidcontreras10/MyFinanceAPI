using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Data
{
	public interface ISpendTypeRepository
	{
		Task<IEnumerable<int>> DeleteSpendTypeUserAsync(string userId, int spendTypeId);
		Task<IEnumerable<int>> AddSpendTypeUserAsync(string userId, int spendTypeId);
		Task<IEnumerable<SpendTypeViewModel>> GetSpendTypeByAccountViewModelsAsync(string userId, int? accountId);
		Task<IEnumerable<SpendTypeViewModel>> GetSpendTypesAsync(string userId, bool includeAll = true);
		Task<IEnumerable<int>> AddEditSpendTypesAsync(string userId, ClientSpendType clientSpendType);
	    Task DeleteSpendTypeAsync(string userId, int spendTypeId);
	}
}
