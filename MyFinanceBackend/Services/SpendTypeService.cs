using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public interface ISpendTypeService
    {
		Task<IEnumerable<int>> DeleteSpendTypeUserAsync(string userId, int spendTypeId);
		Task<IEnumerable<int>> AddSpendTypeUserAsync(string userId, int spendTypeId);
		Task<IEnumerable<SpendTypeViewModel>> GeSpendTypeByAccountViewModelsAsync(string userId, int? accountId);
		Task<IEnumerable<SpendTypeViewModel>> GeSpendTypesAsync(string userId, bool includeAll = true);
        Task<IEnumerable<int>> AddEditSpendTypesAsync(string userId, ClientSpendType clientSpendType);
        Task DeleteSpendTypeAsync(string userId, int spendTypeId);
    }

    public class SpendTypeService :  ISpendTypeService
	{
		#region Attributes

	    private readonly ISpendTypeRepository _spendTypeRepository;

		#endregion

		#region Constructor

		public SpendTypeService(ISpendTypeRepository spendTypeRepository)
		{
			_spendTypeRepository = spendTypeRepository;
		}

		#endregion

		#region Methods

	    public async Task<IEnumerable<int>> DeleteSpendTypeUserAsync(string userId, int spendTypeId)
	    {
		    return await _spendTypeRepository.DeleteSpendTypeUserAsync(userId, spendTypeId);
	    }

	    public async Task<IEnumerable<int>> AddSpendTypeUserAsync(string userId, int spendTypeId)
	    {
		    return await _spendTypeRepository.AddSpendTypeUserAsync(userId, spendTypeId);
	    }

		public async Task<IEnumerable<SpendTypeViewModel>> GeSpendTypeByAccountViewModelsAsync(string userId, int? accountId)
		{
			var result = await _spendTypeRepository.GetSpendTypeByAccountViewModelsAsync(userId, accountId);
			return result;
		}

	    public async Task<IEnumerable<SpendTypeViewModel>> GeSpendTypesAsync(string userId, bool includeAll = true)
	    {
		    var result = await _spendTypeRepository.GetSpendTypesAsync(userId, includeAll);
		    return result;
	    }

        public async Task<IEnumerable<int>> AddEditSpendTypesAsync(string userId, ClientSpendType clientSpendType)
        {
            var result = await _spendTypeRepository.AddEditSpendTypesAsync(userId, clientSpendType);
            return result;
        }

        public async Task DeleteSpendTypeAsync(string userId, int spendTypeId)
        {
            await _spendTypeRepository.DeleteSpendTypeAsync(userId, spendTypeId);
        }

        #endregion
    }
}
