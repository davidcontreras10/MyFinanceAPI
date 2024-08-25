using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	public interface ISpendsService
    {
        Task<IEnumerable<SpendItemModified>> AddNewAppTransactionByAccountAsync(NewAppTransactionByAccount newAppTransactionByAccount);
		Task<IEnumerable<SpendItemModified>> ConfirmPendingTransactionsAsync(IReadOnlyCollection<int> transactionIds, DateTime newPaymentDate);
		Task<IEnumerable<SpendItemModified>> AddIncomeAsync(ClientAddSpendModel clientAddSpendModel);
        Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel);
        Task<IEnumerable<SpendItemModified>> DeleteSpendAsync(string userId, IReadOnlyCollection<int> transactionIds);
        Task<IEnumerable<SpendItemModified>> EditSpendAsync(ClientEditSpendModel model);
        Task<IEnumerable<AccountCurrencyPair>> GetAccountsCurrencyAsync(IEnumerable<int> accountIdsArray);
        Task<IEnumerable<SavedSpend>> GetSavedSpendsAsync(int spendId);
        Task<IEnumerable<SpendItemModified>> ConfirmPendingSpendAsync(int spendId, DateTime newPaymentDate);
        Task<SpendActionResult> GetSpendActionResultAsync(int spendId, ResourceActionNames actionType, ApplicationModules applicationModule);
	    Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModelAsync(IEnumerable<int> accountPeriodIds, string userId);
	    Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModelAsync(int accountPeriodId, int spendId, string userId);
	    Task<IEnumerable<SpendItemModified>> AddBasicTransactionAsync(ClientBasicTrxByPeriod clientBasicTrxByPeriod,
		    TransactionTypeIds transactionTypeId);
    }
}