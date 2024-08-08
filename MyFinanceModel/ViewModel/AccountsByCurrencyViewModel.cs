using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
	public class AccountsByCurrencyViewModel
	{
        public int CurrencyId { get; set; }
        public IReadOnlyCollection<IDropDownSelectable> Accounts { get; set; }
    }
}
