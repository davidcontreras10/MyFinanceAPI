using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel.BankTransactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IBankTransactionsService
	{
		Task<IReadOnlyCollection<BankTrxReqResp>> ProcessAndGetFileBankTransactionState(IReadOnlyCollection<FileBankTransaction> fileBankTransactions, FinancialEntityFile financialEntityFile);
	}
}