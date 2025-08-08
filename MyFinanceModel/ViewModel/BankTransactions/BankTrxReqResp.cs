using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxReqResp
	{
        public IReadOnlyCollection<BankTrxItemReqResp> BankTransactions { get; set; } = [];
        public IReadOnlyCollection<AccountsByCurrencyViewModel> AccountsPerCurrencies { get; set; }
		public FinancialEntityFile FinancialEntityFile { get; set; } = FinancialEntityFile.None;
		public int? FinancialEntityId { get; set; }
	}
}
