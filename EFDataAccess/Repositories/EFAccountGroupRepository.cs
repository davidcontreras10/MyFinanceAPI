using EFDataAccess.Models;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

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
			var guidUserId = new Guid(userId);
			var userAccountGroups = Context.AccountGroup.Where(accg => accg.UserId == guidUserId).ToList();
			var accountGroup = userAccountGroups.FirstOrDefault(accg => accg.AccountGroupId == accountGroupId)
				?? throw new ServiceException("Account group not found.", HttpStatusCode.NotFound);
			if (Context.Account.Any(ac => ac.AccountGroupId == accountGroupId))
			{
				throw new ServiceException("Accounts still associated to this group", HttpStatusCode.BadRequest);
			}

			Context.AccountGroup.Remove(accountGroup);
			AccountGroupOrdering.Renumber(userAccountGroups.Where(accg => !ReferenceEquals(accg, accountGroup)));
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
