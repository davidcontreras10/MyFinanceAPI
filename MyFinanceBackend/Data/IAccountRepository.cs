﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Data
{
	public interface IAccountRepository
	{
		Task<IReadOnlyCollection<Tuple<IdDateTime, AccountPeriodBasicInfo>>> GetAccountPeriodInfoByAccountIdDateTimeAsync(IReadOnlyCollection<IdDateTime> accountsDates);
		Task<IReadOnlyCollection<AccountsByCurrencyViewModel>> GetAccountsByCurrenciesAsync(IEnumerable<int> sourceCurrencyIds, string userId);
		Task<IReadOnlyCollection<AccountPeriodIdReqResp>> GetEquivalentAccountPeriodsByDateAsync(IEnumerable<int> accountPeriodIds, DateTime dateTime);
		Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsPeriodViewModelAsync(string userId, DateTime dateTime);

		Task<AccountPeriodBasicInfo> GetAccountPeriodInfoByAccountIdDateTimeAsync(int accountId, DateTime dateTime);

		Task<IEnumerable<BankAccountPeriodBasicId>> GetBankSummaryAccountsPeriodByUserIdAsync(string userId, DateTime? dateTime);
		Task<IEnumerable<AccountViewModel>> GetOrderedAccountViewModelListAsync(IEnumerable<int> accountIds, string userId);
		IEnumerable<AccountPeriodBasicInfo> GetAccountPeriodBasicInfo(IEnumerable<int> accountPeriodIds);
		AccountPeriodBasicInfo GetAccountPeriodInfoByAccountIdDateTime(int accountId, DateTime dateTime);
		IEnumerable<AccountBasicPeriodInfo> GetAccountBasicInfoByAccountId(IEnumerable<int> accountIds);
		Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(string userId, int? accountGroupId);
		UserAccountsViewModel GetAccountsByUserId(string userId);
		void DeleteAccount(string userId, int accountId);
		Task AddAccountAsync(string userId, ClientAddAccount clientAddAccount);
		Task<AddAccountViewModel> GetAddAccountViewModelAsync(string userId);
		Task<IEnumerable<ItemModified>> UpdateAccountPositionsAsync(string userId,
			IEnumerable<ClientAccountPosition> accountPositions);

		Task UpdateAccountAsync(string userId, ClientEditAccount clientEditAccount);
		IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel(string userId, int currencyId, int? financialEntityId = null);
		IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(IEnumerable<int> accountIds, string userId);
		Task<AccountNotes> UpdateNotes(AccountNotes accountNotes, int accountId);
	}
}