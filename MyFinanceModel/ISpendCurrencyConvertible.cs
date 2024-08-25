using System;
using System.Collections.Generic;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceModel
{
	public interface ISpendCurrencyConvertible
    {
        ClientAddSpendAccount OriginalAccountData { get; }
        IEnumerable<ClientAddSpendAccount> IncludedAccounts { get; }
        int CurrencyId { get; }
        DateTime PaymentDate { get; }
		TransactionTypeIds AmountTypeId { get; }
	}
}
