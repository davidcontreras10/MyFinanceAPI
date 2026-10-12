using EFDataAccess.Models;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceModel.ClientViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace EFDataAccess.Repositories
{
	public class EFAccountGroupRepository : BaseEFRepository, IAccountGroupRepository
	{

		public EFAccountGroupRepository(MyFinanceContext context) : base(context)
		{
		}

		public int AddorEditAccountGroup(AccountGroupClientViewModel accountGroupClientViewModel)
		{
			var userId = new Guid(accountGroupClientViewModel.UserId);
			var isInsert = accountGroupClientViewModel.AccountGroupId == 0;
			var userAccountGroups = Context.AccountGroup.Where(accg => accg.UserId == userId).ToList();
			AccountGroup efAccountGroup = null;
			if (isInsert)
			{
				efAccountGroup = new AccountGroup
				{
					AccountGroupName = accountGroupClientViewModel.AccountGroupName,
					DisplayValue = accountGroupClientViewModel.AccountGroupDisplayValue,
					DisplayDefault = accountGroupClientViewModel.DisplayDefault,
					UserId = userId
				};

				Context.AccountGroup.Add(efAccountGroup);
			}
			else
			{
				efAccountGroup = userAccountGroups.First(x => x.AccountGroupId == accountGroupClientViewModel.AccountGroupId);
				efAccountGroup.AccountGroupName = accountGroupClientViewModel.AccountGroupName;
				efAccountGroup.DisplayDefault = accountGroupClientViewModel.DisplayDefault;
				efAccountGroup.DisplayValue = accountGroupClientViewModel.AccountGroupDisplayValue;
			}

			if (isInsert || accountGroupClientViewModel.AccountGroupPosition != efAccountGroup.AccountGroupPosition)
			{
				AccountGroupOrdering.Place(userAccountGroups, efAccountGroup, accountGroupClientViewModel.AccountGroupPosition);
			}
			else
			{
				AccountGroupOrdering.Renumber(userAccountGroups);
			}

			Context.SaveChanges();
			return isInsert ? efAccountGroup.AccountGroupId : 0;
		}

		public void DeleteAccountGroup(string userId, int accountGroupId)
		{
			if (Context.Account.Any(ac => ac.AccountGroupId == accountGroupId))
			{
				throw new Exception("Accounts still associated to this group");
			}

			Context.AccountGroup.RemoveRange(Context.AccountGroup.Where(accg => accg.AccountGroupId == accountGroupId));
			Context.SaveChanges();
		}

		public IEnumerable<AccountGroupDetailResultSet> GetAccountGroupDetails(string userId, IEnumerable<int> accountGroupIds = null)
		{
			var dbItems = accountGroupIds != null && accountGroupIds.Any()
				? Context.AccountGroup.Where(accgp => accgp.UserId.ToString() == userId && accountGroupIds.Any(id => id == accgp.AccountGroupId))
				: Context.AccountGroup.Where(accgp => accgp.UserId.ToString() == userId);
			return dbItems.Select(x => new AccountGroupDetailResultSet
			{
				AccountGroupDisplayValue = x.DisplayValue,
				AccountGroupId = x.AccountGroupId,
				AccountGroupName = x.AccountGroupName,
				AccountGroupPosition = x.AccountGroupPosition ?? 0,
				DisplayDefault = x.DisplayDefault ?? false
			});
		}
	}
}
