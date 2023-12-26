using MyFinanceBackend.Models;
using System.Collections.Generic;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Data
{
	public interface IAccountGroupRepository
	{
		void DeleteAccountGroup(string userId, int accountGroupId);
		IEnumerable<AccountGroupDetailResultSet> GetAccountGroupDetails(string userId, IEnumerable<int> accountGroupIds = null);
	    int AddorEditAccountGroup(AccountGroupClientViewModel accountGroupClientViewModel);
	}
}
