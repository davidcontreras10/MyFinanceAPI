using System;
using System.Collections.Generic;
using System.Linq;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	public class AccountGroupService : IAccountGroupService
	{
		#region Attributes

		private readonly IAccountGroupRepository _accountGroupRepository;

		#endregion

		#region Constructor

		public AccountGroupService(IAccountGroupRepository accountGroupRepository)
		{
			_accountGroupRepository = accountGroupRepository;
		}

		#endregion

		#region Public Methods

		public void DeleteAccountGroup(string userId, int accountGroupId)
		{
			_accountGroupRepository.DeleteAccountGroup(userId, accountGroupId);
		}

	    public int AddorEditAccountGroup(AccountGroupClientViewModel accountGroupClientViewModel)
	    {
	        if (accountGroupClientViewModel == null)
	        {
	            throw new ArgumentNullException("accountGroupClientViewModel");
	        }

	        return _accountGroupRepository.AddorEditAccountGroup(accountGroupClientViewModel);
	    } 

		public IEnumerable<AccountGroupDetailViewModel> GetAccountGroupDetailViewModel(string userId, IEnumerable<int> accountGroupIds)
		{
			var resultSet = _accountGroupRepository.GetAccountGroupDetails(userId, accountGroupIds);
			var result = resultSet.Select(CreateAccountGroupDetailViewModel);
			return result;
		}

		public IEnumerable<AccountGroupViewModel> GetAccountGroupViewModel(string userId)
		{
			var resultSets = _accountGroupRepository.GetAccountGroupDetails(userId);
			var result = resultSets.Select(CreateAccountGroupViewModel);
			return result;
		}

		public IEnumerable<AccountGroupPosition> GetAccountGroupPositions(string userId, bool validateAdd = false,
			int accountGroupIdSelected = 0)
		{
			var list = GetAccountGroupViewModel(userId);
			var positions = list.Select(CreateAccountGroupPosition).ToList();
			if (validateAdd && accountGroupIdSelected == 0)
			{
				var lastPosition = list.OrderByDescending(i => i.AccountGroupPosition).First().AccountGroupPosition;
				var newPosition = lastPosition + 1;
				positions.Add(new AccountGroupPosition
				{
					AccountGroupId = 0,
					IsSelected = true,
					Name = newPosition.ToString(),
					Position = newPosition
				});
			}

			positions.ForEach(item => item.IsSelected = (item.AccountGroupId == accountGroupIdSelected));
			return positions.OrderBy(item => item.Position);
		}

		#endregion

		#region Private Methods

		public static AccountGroupPosition CreateAccountGroupPosition(AccountGroupViewModel accountGroupViewModel)
		{
			return new AccountGroupPosition
			{
				AccountGroupId = accountGroupViewModel.AccountGroupId,
				Name = accountGroupViewModel.AccountGroupPosition.ToString(),
				Position = accountGroupViewModel.AccountGroupPosition
			};
		}

		public static AccountGroupViewModel CreateAccountGroupViewModel(
			AccountGroupDetailResultSet accountGroupDetailResultSet)
		{
			return new AccountGroupViewModel
			{
				AccountGroupId = accountGroupDetailResultSet.AccountGroupId,
				AccountGroupName = accountGroupDetailResultSet.AccountGroupName,
				AccountGroupPosition = accountGroupDetailResultSet.AccountGroupPosition,
				IsSelected = accountGroupDetailResultSet.DisplayDefault
			};
		}

		public static AccountGroupDetailViewModel CreateAccountGroupDetailViewModel(
			AccountGroupDetailResultSet accountGroupDetailResultSet)
		{
			if (accountGroupDetailResultSet == null)
			{
				throw new ArgumentNullException("accountGroupDetailResultSet");
			}

			return new AccountGroupDetailViewModel
			{
				AccountGroupId = accountGroupDetailResultSet.AccountGroupId,
				AccountGroupDisplayValue = accountGroupDetailResultSet.AccountGroupDisplayValue,
				AccountGroupName = accountGroupDetailResultSet.AccountGroupName,
				AccountGroupPosition = accountGroupDetailResultSet.AccountGroupPosition,
				DisplayDefault = accountGroupDetailResultSet.DisplayDefault
			};
		}

		#endregion
	}
}
