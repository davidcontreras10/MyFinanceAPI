using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Data
{
	public interface ISpendsRepository : ITransactional
    {
        Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions);
		Task<IReadOnlyCollection<int>> AppTransactionsWithBankTrxAsync(IEnumerable<int> trxIds);
		Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModelAsync(IEnumerable<int> accountPeriodIds, string userId);
		Task<SpendOnPeriodId> GetSpendOnPeriodAccountBySpendIdAccountIdAsync(int spendId, int accountId);
	    Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModelAsync(int accountPeriodId, int spendId, string userId);
		Task<AccountFinanceViewModel> GetAccountFinanceViewModelAsync(int accountPeriodId, string userId);
		Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModelAsync(IEnumerable<ClientAccountFinanceViewModel> requestItems, string userId, DateTime? dateTime);
		Task<IEnumerable<SpendItemModified>> DeleteTransactionsAsync(IReadOnlyCollection<int> transactionIds);
        Task<IEnumerable<SpendItemModified>> EditSpendAsync(ClientEditSpendModel model);
        Task<IEnumerable<AccountCurrencyPair>> GetAccountsCurrencyAsync(IEnumerable<int> accountIdsArray);

        [Obsolete("Use AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions) instead")]
        Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel);
        Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId);
		Task<IEnumerable<SavedSpend>> GetSavedSpendsAsync(int spendId);
        Task<IEnumerable<SpendItemModified>> EditSpendAsync(FinanceSpend financeSpend);
        Task<IEnumerable<ClientAddSpendAccount>> GetAccountMethodConversionInfoAsync(int? accountId, int? accountPeriodId,
            string userId, int currencyId);
        Task<ClientAddSpendModel> CreateClientAddSpendModelAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId);
        Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrenciesAsync(int accountId, string userId);
        Task AddSpendDependencyAsync(int spendId, int dependencySpendId);
        Task<SpendActionAttributes> GetSpendAttributesAsync(int spendId);
    }
}
