using System.Collections.Generic;
using System.Linq;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	/// <summary>
	/// Account types suggested when creating an account: Bank for a main account, Saving for a sub-account.
	/// They are only suggestions (the user can pick another type) and use the account type codes of
	/// <see cref="FullAccountInfoViewModel.AccountType"/>. A type that isn't active isn't suggested.
	/// </summary>
	public static class AccountTypeSuggestions
	{
		public static int? ForMainAccount(IEnumerable<int> activeAccountTypeIds)
		{
			return Suggest(FullAccountInfoViewModel.AccountType.Bank, activeAccountTypeIds);
		}

		public static int? ForSubAccount(IEnumerable<int> activeAccountTypeIds)
		{
			return Suggest(FullAccountInfoViewModel.AccountType.Saving, activeAccountTypeIds);
		}

		private static int? Suggest(FullAccountInfoViewModel.AccountType accountType, IEnumerable<int> activeAccountTypeIds)
		{
			var id = (int)accountType;
			return activeAccountTypeIds.Contains(id) ? id : null;
		}
	}
}
