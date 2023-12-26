using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface ITransferRepository : ITransactional
	{
		Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccountAsync(int accountPeriodId, int currencyId, string userId);
		Task<int> GetDefaultCurrencyConvertionMethodsAsync(int originAccountId, int amountCurrencyId, int destinationCurrencyId,
			string userId);
		Task AddTransferRecordAsync(IEnumerable<int> spendIds, string userId);
	}
}
