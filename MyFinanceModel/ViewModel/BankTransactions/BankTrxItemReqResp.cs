using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxItemReqResp
	{
        public FileBankTransaction FileTransaction { get; set; }

        public BankTransactionStatus DbStatus { get; set; }

        public CurrencyViewModel Currency { get; set; }

        public DbData ProcessData { get; set; }

        public class DbData
		{
            public IReadOnlyCollection<SpendViewModel> Transactions { get; set; }
		}
	}
}
