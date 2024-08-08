using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxReqResp
	{
        public IReadOnlyCollection<BankTrxItemReqResp> BankTransactions { get; set; } = Array.Empty<BankTrxItemReqResp>();
        public IReadOnlyCollection<AccountsByCurrencyViewModel> AccountsPerCurrencies { get; set; }
    }
}
