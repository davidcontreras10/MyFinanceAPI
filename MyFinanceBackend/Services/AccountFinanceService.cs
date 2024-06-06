using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	public class AccountFinanceService(ISpendsRepository spendsRepository, IAccountRepository accountRepository) : IAccountFinanceService
	{
		private readonly ISpendsRepository _spendsRepository = spendsRepository;
		private readonly IAccountRepository _accountRepository = accountRepository;

		#region Publics

		public async Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModelAsync(IEnumerable<ClientAccountFinanceViewModel> requestItems
			, string userId
			, DateTime? expectedDate = null)
		{
			if (expectedDate != null)
			{
				var newAccountPeriods = await _accountRepository.GetEquivalentAccountPeriodsByDateAsync(requestItems.Select(i => i.AccountPeriodId), expectedDate.Value);
				var newItems = requestItems.ToList();
				foreach (var accountPeriod in newAccountPeriods)
				{
					var requestItem = requestItems.FirstOrDefault(ri => ri.AccountPeriodId == accountPeriod.AccountPeriodIdReq);
					if (requestItem != null)
					{
						if (accountPeriod.AccountPeriodResp == null)
						{
							newItems.Remove(requestItem);
						}
						else
						{
							requestItem.AccountPeriodId = accountPeriod.AccountPeriodResp.Value;
						}
					}
				}

				requestItems = newItems;
			}
			return await _spendsRepository.GetAccountFinanceViewModelAsync(requestItems, userId);
		}

		public async Task<IEnumerable<BankAccountSummary>> GetAccountFinanceSummaryViewModelAsync(string userId, DateTime? dateTime = null)
		{
			var bankAccounts = await _accountRepository.GetBankSummaryAccountsPeriodByUserIdAsync(userId, dateTime);
			if (bankAccounts == null || !bankAccounts.Any())
			{
				return Array.Empty<BankAccountSummary>();
			}

			var requestItems = bankAccounts.Select(acc => CreateBankAccountClientAccountFinanceRequest(acc.AccountPeriodId));
			var financeInfoAccounts = await GetAccountFinanceViewModelAsync(requestItems, userId);
			return financeInfoAccounts.Select(fa => CreateBankAccountSummary(fa, bankAccounts));
		}

		#endregion

		#region Privates

		private static BankAccountSummary CreateBankAccountSummary(AccountFinanceViewModel accountFinanceViewModel, IEnumerable<BankAccountPeriodBasicId> bankAccounts)
		{
			if (accountFinanceViewModel == null)
			{
				throw new ArgumentNullException(nameof(accountFinanceViewModel));
			}

			var bankAccount = bankAccounts?.FirstOrDefault(ba => ba.AccountId == accountFinanceViewModel.AccountId);
			return new BankAccountSummary
			{
				AccountId = accountFinanceViewModel.AccountId,
				AccountName = accountFinanceViewModel.AccountName,
				FinancialEntityId = bankAccount?.FinancialEntityId,
				FinancialEntityName = bankAccount?.FinancialEntityName,
				Balance = new CurrencyAmount
				{
					Amount = accountFinanceViewModel.GeneralBalanceToday,
					CurrencySymbol = accountFinanceViewModel.CurrencySymbol
				}
			};
		}

		private static ClientAccountFinanceViewModel CreateBankAccountClientAccountFinanceRequest(int accountPeriodId)
		{
			return new ClientAccountFinanceViewModel
			{
				AccountPeriodId = accountPeriodId,
				AmountTypeId = 0,
				LoanSpends = true,
				PendingSpends = false
			};
		}

		#endregion
	}
}