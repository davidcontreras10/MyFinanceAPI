using System.Collections.Generic;
using MyFinanceModel.ViewModel;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Services
{
	public class AccountService : IAccountService
	{
		#region Constructor

		public AccountService(IAccountRepository accountRepository)
		{
		    _accountRepository = accountRepository;
		}

        #endregion

        #region Attributes

	    private readonly IAccountRepository _accountRepository;

		#endregion

		#region Public methods

		public async Task<AiClassifiableAccount> GetAiClassificationHintAsync(string userId, int accountId)
		{
			if (!Guid.TryParse(userId, out _))
				throw new ServiceException("Authentication is required.", HttpStatusCode.Unauthorized);
			if (accountId <= 0)
				throw new ServiceException("A valid account ID is required.", HttpStatusCode.BadRequest);
			return await _accountRepository.GetAiClassificationHintAsync(userId, accountId)
				?? throw new ServiceException("Account not found.", HttpStatusCode.NotFound);
		}

		public async Task<AiClassifiableAccount> UpdateAiClassificationHintAsync(string userId, int accountId, ClientAccountAiClassificationHint request)
		{
			if (!Guid.TryParse(userId, out _))
				throw new ServiceException("Authentication is required.", HttpStatusCode.Unauthorized);
			if (accountId <= 0 || request == null)
				throw new ServiceException("A valid account ID and hint request are required.", HttpStatusCode.BadRequest);
			if (request.AiClassificationHint?.Length > ClientAccountAiClassificationHint.MaxHintLength)
				throw new ServiceException($"The AI classification hint must not exceed {ClientAccountAiClassificationHint.MaxHintLength} characters.", HttpStatusCode.BadRequest);
			var hint = string.IsNullOrWhiteSpace(request.AiClassificationHint) ? null : request.AiClassificationHint.Trim();
			return await _accountRepository.UpdateAiClassificationHintAsync(userId, accountId, hint)
				?? throw new ServiceException("Account not found.", HttpStatusCode.NotFound);
		}

		public async Task<IReadOnlyCollection<AccountsByCurrencyViewModel>> GetAccountsByCurrenciesAsync(IEnumerable<int> sourceCurrencyIds, string userId)
		{
			return await _accountRepository.GetAccountsByCurrenciesAsync(sourceCurrencyIds, userId);
		}

		public async Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsPeriodViewModelAsync(
			string userId,
			DateTime dateTime
		)
		{
			return await _accountRepository.GetAccountDetailsPeriodViewModelAsync(userId, dateTime);
		}

		public UserAccountsViewModel GetAccountsByUserId(string userId)
		{
			if (string.IsNullOrEmpty(userId))
			{
				throw new ArgumentNullException(nameof(userId));
			}

			return _accountRepository.GetAccountsByUserId(userId);
		}

        public void DeleteAccount(string userId, int accountId)
        {
	        _accountRepository.DeleteAccount(userId, accountId);
        }

        public async Task AddAccountAsync(string userId, ClientAddAccount clientAddAccount)
        {
	        await ValidateAccountTypeAsync(null, (int)clientAddAccount.AccountTypeId);
	        await ValidateParentAccountsAsync(userId, null, clientAddAccount.AccountIncludes);
	        await ApplyMainAccountRulesAsync(clientAddAccount);
	        await _accountRepository.AddAccountAsync(userId, clientAddAccount);
        }

		public async Task<AddAccountViewModel> GetAddAccountViewModelAsync(string userId)
		{
			return await _accountRepository.GetAddAccountViewModelAsync(userId);
		}

	    public async Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(string userId, int? accountGroupId)
	    {
	        return await _accountRepository.GetAccountDetailsViewModelAsync(userId, accountGroupId);
	    }

	    public async Task<IEnumerable<ItemModified>> UpdateAccountPositionsAsync(string userId,
			IEnumerable<ClientAccountPosition> accountPositions)
	    {
		    return await _accountRepository.UpdateAccountPositionsAsync(userId, accountPositions);
	    }

		public async Task UpdateAccountAsync(string userId, ClientEditAccount clientEditAccount)
		{
			if (clientEditAccount.EditAccountFields?.Contains(AccountFiedlds.AccountTypeId) == true)
			{
				await ValidateAccountTypeAsync(clientEditAccount.AccountId, (int)clientEditAccount.AccountTypeId);
			}

			if (clientEditAccount.EditAccountFields?.Contains(AccountFiedlds.AccountIncludes) == true)
			{
				await ValidateParentAccountsAsync(userId, clientEditAccount.AccountId, clientEditAccount.AccountIncludes);
			}

			await _accountRepository.UpdateAccountAsync(userId, clientEditAccount);
		}

		public IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel(string userId, int currencyId, int? financialEntityId = null)
		{
			return _accountRepository.GetAccountIncludeViewModel(userId, currencyId, financialEntityId);
		}

		public IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(IEnumerable<int> accountIds, string userId)
		{
			return _accountRepository.GetAccountDetailsViewModel(accountIds, userId);
		}

		public async Task<AccountNotes> UpdateNotes(string userId, AccountNotes accountNotes, int accountId)
		{
			return await _accountRepository.UpdateNotes(userId, accountNotes, accountId);
		}

		#endregion

		#region Private methods

		/// <summary>
		/// A new sub-account must follow <see cref="AccountLinkRules"/>: same financial entity as its main
		/// account and the right conversion method, which the server decides (see ResolveMethodId).
		/// Only applied when creating accounts; existing accounts are not re-checked on edit.
		/// </summary>
		private async Task ApplyMainAccountRulesAsync(ClientAddAccount clientAddAccount)
		{
			var link = clientAddAccount.AccountIncludes?.SingleOrDefault();
			if (link == null)
			{
				return;
			}

			var context = await _accountRepository.GetAccountLinkContextAsync(link.AccountIncludeId);
			if (context.ParentCurrencyId == null)
			{
				throw new ServiceException("The main account has no currency.", System.Net.HttpStatusCode.BadRequest);
			}

			int? childFinancialEntityId = clientAddAccount.FinancialEntityId > 0 ? clientAddAccount.FinancialEntityId : null;
			int? requestedMethodId = link.CurrencyConverterMethodId > 0 ? link.CurrencyConverterMethodId : null;
			link.CurrencyConverterMethodId = AccountLinkRules.ResolveMethodId(
				clientAddAccount.CurrencyId,
				childFinancialEntityId,
				context.ParentCurrencyId.Value,
				context.ParentFinancialEntityId,
				requestedMethodId,
				context.Methods);
		}

		private async Task ValidateAccountTypeAsync(int? accountId, int accountTypeId)
		{
			var usage = await _accountRepository.GetAccountTypeUsageAsync(accountTypeId, accountId);
			AccountTypeRules.ValidateSelectable(usage);
		}

		private async Task ValidateParentAccountsAsync(string userId, int? accountId, IEnumerable<ClientAccountInclude> accountIncludes)
		{
			var requestedParentIds = accountIncludes?.Select(x => x.AccountIncludeId).ToList() ?? [];
			if (requestedParentIds.Count == 0)
			{
				return;
			}

			// Fail fast on the cheapest rule before touching the database.
			if (requestedParentIds.Count > 1)
			{
				AccountHierarchyValidator.ValidateParents(accountId, requestedParentIds, new HashSet<int>(), new HashSet<int>(), 0);
			}

			var info = await _accountRepository.GetAccountHierarchyInfoAsync(userId, accountId, requestedParentIds);
			AccountHierarchyValidator.ValidateParents(accountId, requestedParentIds, info.OwnedAccountIds, info.AccountIdsWithParent, info.SubAccountsCount);
		}

		#endregion
	}
}
