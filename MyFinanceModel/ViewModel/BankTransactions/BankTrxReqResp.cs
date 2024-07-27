using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxReqResp
	{
        public FileBankTransaction Transaction { get; set; }

        public BankTransactionStatus DbStatus { get; set; }
    }
}
