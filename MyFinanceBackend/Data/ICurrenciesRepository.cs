using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface ICurrenciesRepository
	{
		Task<IReadOnlyCollection<CurrencyViewModel>> GetCurrenciesByCodesAsync(IEnumerable<string> codes);
		Task<IReadOnlyCollection<BasicCurrencyViewModel>> GetCurrenciesAsync();
		Task<int?> GetCurrencyConverterMethodIdAsync(int sourceCurrencyId, int destinationCurrencyId, int? financialEntityId);
	}
}
