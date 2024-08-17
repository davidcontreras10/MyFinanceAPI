using MyFinanceModel.Records;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
	public class AccountsByCurrencyViewModel
	{
        public int CurrencyId { get; set; }
        public IReadOnlyCollection<AccountWithTrxTypeId> Accounts { get; set; }
    }
}
