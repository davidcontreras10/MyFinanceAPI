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
			AccountGroup efAccountGroup = null;
			if (isInsert)
			{
				efAccountGroup = new AccountGroup
				{
					AccountGroupName = accountGroupClientViewModel.AccountGroupName,
					DisplayValue = accountGroupClientViewModel.AccountGroupDisplayValue,
					AccountGroupPosition = accountGroupClientViewModel.AccountGroupPosition,
					DisplayDefault = accountGroupClientViewModel.DisplayDefault,
					UserId = userId
				};

				var insertedEntity = Context.AccountGroup.Add(efAccountGroup);
			}
			else
			{
				efAccountGroup = Context.AccountGroup.First(x => x.AccountGroupId == accountGroupClientViewModel.AccountGroupId);
				efAccountGroup.AccountGroupName = accountGroupClientViewModel.AccountGroupName;
				efAccountGroup.DisplayDefault = accountGroupClientViewModel.DisplayDefault;
				efAccountGroup.DisplayValue = accountGroupClientViewModel.AccountGroupDisplayValue;
			}

			var userAccountGroups = Context.AccountGroup.Where(accg => accg.UserId == userId);
			if (isInsert || accountGroupClientViewModel.AccountGroupPosition != efAccountGroup.AccountGroupPosition)
			{
				efAccountGroup.AccountGroupPosition = accountGroupClientViewModel.AccountGroupPosition;
				var maxPosition = userAccountGroups.Where(accg => accg.UserId == userId).Max(x => x.AccountGroupPosition) ?? 0;
				if (efAccountGroup.AccountGroupPosition >= maxPosition)
				{
					efAccountGroup.AccountGroupPosition = maxPosition + 1;
				}
				else
				{
					var updatePos = userAccountGroups
						.Where(accg => accg.AccountGroupId != accountGroupClientViewModel.AccountGroupId
							&& accg.AccountGroupPosition >= accountGroupClientViewModel.AccountGroupPosition);
					foreach (var accg in updatePos)
					{
						accg.AccountGroupPosition += 1;
					}
				}
			}

			UpdatePositions(userAccountGroups);
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

		private void UpdatePositions(IEnumerable<AccountGroup> userAccountGroups = null, Guid? userId = null)
		{
			if ((userAccountGroups == null || !userAccountGroups.Any()) && userId == null)
			{
				throw new ArgumentException($"Either {nameof(userAccountGroups)} or {nameof(userId)} must be provided");
			}

			var updatePositions = userAccountGroups != null && userAccountGroups.Any()
				? userAccountGroups.OrderBy(accg => accg.AccountGroupPosition)
				: (IEnumerable<AccountGroup>)Context.AccountGroup.Where(accg => accg.UserId == userId).OrderBy(accg => accg.AccountGroupPosition);
			var pos = 1;
			Debug.WriteLine("UpdatePositions");
			foreach (var positionAccg in updatePositions)
			{
				positionAccg.AccountGroupPosition = pos++;
				Debug.WriteLine($"Accg {positionAccg.AccountGroupName}: {positionAccg.AccountGroupPosition}");
			}
		}
	}
}
