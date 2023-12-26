using System;
using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Services
{
    public interface IAccountService
    {
	    Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsPeriodViewModelAsync(
		    string userId,
		    DateTime dateTime
	    );
		UserAccountsViewModel GetAccountsByUserId(string userId);
		IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(IEnumerable<int> accountIds, string userId);
        IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel(string userId, int currencyId);
        Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(string userId, int? accountGroupId);
	    Task<IEnumerable<ItemModified>> UpdateAccountPositionsAsync(string userId, IEnumerable<ClientAccountPosition> accountPositions);
	    Task UpdateAccountAsync(string userId, ClientEditAccount clientEditAccount);
	    Task<AddAccountViewModel> GetAddAccountViewModelAsync(string userId);
        void AddAccount(string userId, ClientAddAccount clientAddAccount);
        void DeleteAccount(string userId, int accountId);
		Task<AccountNotes> UpdateNotes(AccountNotes accountNotes, int accountId);
	}
}