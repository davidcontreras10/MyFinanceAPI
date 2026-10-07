using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using MyFinanceWebApiCore.Services;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountsController : BaseApiController
	{
		#region Attributes

		private readonly IAccountService _accountService;
		private readonly IAccountFinanceService _accountFinanceService;
		private readonly IAccountAuthorizationService _authorizationService;

		#endregion

		#region Constructor

		public AccountsController(IAccountService accountService, IAccountFinanceService accountFinanceService,
			IAccountAuthorizationService authorizationService)
		{
			_accountService = accountService;
			_accountFinanceService = accountFinanceService;
			_authorizationService = authorizationService;
		}

		#endregion

		#region Routes

		[HttpGet("{accountId:int}/ai-classification-hint")]
		public async Task<AiClassifiableAccount> GetAiClassificationHint([FromRoute] int accountId)
		{
			ValidateHintAccountId(accountId);
			var scope = await AuthorizeAsync(ResourceActionNames.View, new AccountAccessTargets { AccountIds = new[] { accountId } }, System.Net.HttpStatusCode.NotFound);
			return await _accountService.GetAiClassificationHintAsync(scope.OwnerUserId, accountId);
		}

		[HttpPut("{accountId:int}/ai-classification-hint")]
		public async Task<AiClassifiableAccount> UpdateAiClassificationHint(
			[FromRoute] int accountId, [FromBody] ClientAccountAiClassificationHint request)
		{
			ValidateHintAccountId(accountId);
			var scope = await AuthorizeAsync(ResourceActionNames.Edit, new AccountAccessTargets { AccountIds = new[] { accountId } }, System.Net.HttpStatusCode.NotFound);
			if (!ModelState.IsValid)
				throw new ServiceException("Provide aiClassificationHint as text or null, with at most 4000 characters.", System.Net.HttpStatusCode.BadRequest);
			return await _accountService.UpdateAiClassificationHintAsync(scope.OwnerUserId, accountId, request);
		}

		[HttpGet]
		[Route("currencies/addition")]
		public async Task<IReadOnlyCollection<AccountsByCurrencyViewModel>> GetAccountsByCurrenciesAsync([FromQuery]int[] sourceCurrencyIds)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View)).OwnerUserId;
			var res = await _accountService.GetAccountsByCurrenciesAsync(sourceCurrencyIds, userId);
			return res;
		}

		[HttpDelete("{accountId}")]
		public async Task DeleteAccountV2([FromRoute] int accountId)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.Delete, new AccountAccessTargets { AccountIds = new[] { accountId } })).OwnerUserId;
			_accountService.DeleteAccount(userId, accountId);
		}

		[Obsolete("Use DeleteAccountV2 instead.")]
		[HttpDelete]
		public async Task DeleteAccount([FromQuery] int accountId)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.Delete, new AccountAccessTargets { AccountIds = new[] { accountId } })).OwnerUserId;
			_accountService.DeleteAccount(userId, accountId);
		}

		//[ValidateModelState]
		[HttpPost]
		public async Task AddAccount([FromBody] ClientAddAccount clientAddAccount)
		{
			ValidateUserIdClaim();
			if (clientAddAccount == null)
				throw new ServiceException("An account request is required.", System.Net.HttpStatusCode.BadRequest);
			var userId = (await AuthorizeAsync(ResourceActionNames.Add, new AccountAccessTargets
			{
				AccountIds = ParentIds(clientAddAccount.AccountIncludes),
				AccountGroupIds = new[] { clientAddAccount.AccountGroupId },
				SpendTypeIds = OptionalId(clientAddAccount.SpendTypeId)
			})).OwnerUserId;
			await _accountService.AddAccountAsync(userId, clientAddAccount);
		}

		[Route("{accountId}/notes")]
		[HttpPost]
		public async Task<AccountNotes> UpdateAccountNotes([FromRoute] int accountId, [FromBody]AccountNotes accountNotes)
		{
			var scope = await AuthorizeAsync(ResourceActionNames.Edit, new AccountAccessTargets { AccountIds = new[] { accountId } });
			if (accountNotes == null)
				throw new ServiceException("Account notes are required.", System.Net.HttpStatusCode.BadRequest);
			return await _accountService.UpdateNotes(scope.OwnerUserId, accountNotes, accountId);
		}

		[Route("finance")]
		[HttpPost]
		public async Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModel([FromBody] ClientAccountFinanceViewModel[] accountPeriods
			, [FromQuery]DateTime? expectedDate = null)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View, FinanceTargets(accountPeriods))).OwnerUserId;
			var accountFinanceViewModelList = await _accountFinanceService.GetAccountFinanceViewModelAsync(accountPeriods, userId, expectedDate);
			return accountFinanceViewModelList;
		}

		[Route("finance/excel")]
		[HttpPost]
		public async Task<FileContentResult> GetExcelAccountFinanceViewModel([FromBody] ClientAccountFinanceViewModel[] accountPeriods)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View, FinanceTargets(accountPeriods))).OwnerUserId;
			var accountFinanceViewModelList = await _accountFinanceService.GetAccountFinanceViewModelAsync(accountPeriods, userId);
			var bytes = ExcelFileHelper.GenerateFile(accountFinanceViewModelList.ToList());
			return File(bytes, "application/octet-stream", ExcelFileHelper.GetFileName(accountFinanceViewModelList));
		}

		[Route("finance/summary")]
		[HttpGet]
		public async Task<IEnumerable<BankAccountSummary>> GetAccountFinanceSummaryViewModel()
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View)).OwnerUserId;
			var accounts = await _accountFinanceService.GetAccountFinanceSummaryViewModelAsync(userId);
			return accounts;
		}

		[HttpGet]
		[Route("user")]
		public async Task<UserAccountsViewModel> GetAccountsByUserId()
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View)).OwnerUserId;
			var accounts = _accountService.GetAccountsByUserId(userId);
			return accounts;
		}

		[Route("list")]
		[HttpGet]
		public async Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsViewModel()
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View)).OwnerUserId;
			return await _accountService.GetAccountDetailsPeriodViewModelAsync(userId, DateTime.UtcNow);
		}

		[Route("{accountGroupId}")]
		[HttpGet]
		public async Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(int? accountGroupId = null)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View, new AccountAccessTargets
			{
				AccountGroupIds = accountGroupId > 0 ? new[] { accountGroupId.Value } : Array.Empty<int>()
			})).OwnerUserId;
			var result = await _accountService.GetAccountDetailsViewModelAsync(userId, accountGroupId);
			return result;
		}

		[HttpGet]
		public async Task<IEnumerable<AccountDetailsInfoViewModel>> GetAccountDetailsInfoViewModel([FromQuery] int[] accountIds)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View, new AccountAccessTargets { AccountIds = accountIds })).OwnerUserId;
			var result = _accountService.GetAccountDetailsViewModel(accountIds, userId);
			return result;
		}

		[Route("include/{currencyId}")]
		[HttpGet]
		public async Task<IEnumerable<AccountIncludeViewModel>> GetAccountIncludeViewModel([FromRoute]int currencyId, [FromQuery]int? financialEntityId = null)
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.View)).OwnerUserId;
			if(financialEntityId != null && financialEntityId < 1)
			{
				financialEntityId = null;
			}
			var result = _accountService.GetAccountIncludeViewModel(userId, currencyId, financialEntityId);
			return result;
		}

		[Route("add")]
		[HttpGet]
		public async Task<AddAccountViewModel> GetAddAccountViewModel()
		{
			var userId = (await AuthorizeAsync(ResourceActionNames.Add)).OwnerUserId;
			var result = await _accountService.GetAddAccountViewModelAsync(userId);
			return result;
		}

		[Route("positions")]
		[HttpPut]
		public async Task<IEnumerable<ItemModified>> UpdateAccountPositions([FromBody] IEnumerable<ClientAccountPosition> accountPositions)
		{
			var positions = accountPositions?.ToArray();
			var userId = (await AuthorizeAsync(ResourceActionNames.Edit, new AccountAccessTargets { AccountIds = positions?.Select(p => p?.AccountId ?? 0).ToArray() })).OwnerUserId;
			var result = await _accountService.UpdateAccountPositionsAsync(userId, positions);
			return result;
		}

		[HttpPatch]
		public async Task UpdateAccount([FromBody] ClientEditAccount clientEditAccount)
		{
			ValidateUserIdClaim();
			if (clientEditAccount?.EditAccountFields == null)
				throw new ServiceException("Account fields are required.", System.Net.HttpStatusCode.BadRequest);
			var fields = clientEditAccount.EditAccountFields.ToHashSet();
			var userId = (await AuthorizeAsync(ResourceActionNames.Edit, new AccountAccessTargets
			{
				AccountIds = new[] { clientEditAccount.AccountId }.Concat(fields.Contains(AccountFiedlds.AccountIncludes) ? ParentIds(clientEditAccount.AccountIncludes) : Array.Empty<int>()).ToArray(),
				AccountGroupIds = fields.Contains(AccountFiedlds.AccountGroupId) ? new[] { clientEditAccount.AccountGroupId } : Array.Empty<int>(),
				SpendTypeIds = fields.Contains(AccountFiedlds.SpendTypeId) ? OptionalId(clientEditAccount.SpendTypeId) : Array.Empty<int>()
			})).OwnerUserId;
			await _accountService.UpdateAccountAsync(userId, clientEditAccount);
		}

		#endregion

		private async Task<AccountAccessScope> AuthorizeAsync(ResourceActionNames action, AccountAccessTargets targets = null,
			System.Net.HttpStatusCode denialStatus = System.Net.HttpStatusCode.Forbidden)
		{
			var result = await _authorizationService.AuthorizeAsync(GetUserId(), action, targets ?? new AccountAccessTargets());
			if (result?.IsAuthorized != true || result.Scope == null || result.Scope.OwnerId == Guid.Empty)
				throw new ServiceException(denialStatus == System.Net.HttpStatusCode.NotFound ? "Account not found." : "Account access is denied.", denialStatus);
			return result.Scope;
		}

		private void ValidateHintAccountId(int accountId)
		{
			ValidateUserIdClaim();
			if (accountId <= 0)
				throw new ServiceException("A valid account ID is required.", System.Net.HttpStatusCode.BadRequest);
		}

		private static int[] OptionalId(int? id) => id.HasValue ? new[] { id.Value } : Array.Empty<int>();
		private static int[] ParentIds(IEnumerable<ClientAccountInclude> links) => links?.Select(link => link?.AccountIncludeId ?? 0).ToArray() ?? Array.Empty<int>();
		private static AccountAccessTargets FinanceTargets(IEnumerable<ClientAccountFinanceViewModel> items) =>
			new AccountAccessTargets { AccountPeriodIds = items?.Select(item => item?.AccountPeriodId ?? 0).ToArray() };
	}
}
