using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel.BankTransactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IBankTransactionsService
	{
		Task<BankTrxReqResp> GetBankTransactionBySearchCriteriaAsync(IUserSearchCriteria userSearchCriteria);
		Task DeleteBankTransactionAsync(BankTrxId bankTrxId);
		Task<UserProcessingResponse> ResetBankTransactionAsync(BankTrxId bankTrxId);
		Task<UserProcessingResponse> ProcessUserBankTrxAsync(string userId, IReadOnlyCollection<BankItemRequest> bankItemRequests);
		Task<BankTrxReqResp> InsertAndGetFileBankTransactionState(IReadOnlyCollection<FileBankTransaction> fileBankTransactions, FinancialEntityFile financialEntityFile, string userId);
	}
}