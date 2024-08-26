using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
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

		#endregion

		#region Constructor

		public AccountsController(IAccountService accountService, IAccountFinanceService accountFinanceService)
		{
			_accountService = accountService;
			_accountFinanceService = accountFinanceService;
		}

		#endregion

		#region Routes

		[HttpGet]
		[Route("currencies/addition")]
		public async Task<IReadOnlyCollection<AccountsByCurrencyViewModel>> GetAccountsByCurrenciesAsync([FromQuery]int[] sourceCurrencyIds)
		{
			var userId = GetUserId();
			var res = await _accountService.GetAccountsByCurrenciesAsync(sourceCurrencyIds, userId);
			return res;
		}

		[HttpDelete("{accountId}")]
		public void DeleteAccountV2([FromRoute] int accountId)
		{
			var userId = GetUserId();
			_accountService.DeleteAccount(userId, accountId);
		}

		[Obsolete("Use DeleteAccountV2 instead.")]
		[HttpDelete]
		public void DeleteAccount([FromQuery] int accountId)
		{
			var userId = GetUserId();
			_accountService.DeleteAccount(userId, accountId);
		}

		//[ValidateModelState]
		[HttpPost]
		public void AddAccount([FromBody] ClientAddAccount clientAddAccount)
		{
			var userId = GetUserId();
			_accountService.AddAccount(userId, clientAddAccount);
		}

		[Route("{accountId}/notes")]
		[HttpPost]
		public async Task<AccountNotes> UpdateAccountNotes([FromRoute] int accountId, [FromBody]AccountNotes accountNotes)
		{
			return await _accountService.UpdateNotes(accountNotes, accountId);
		}

		[Route("finance")]
		[HttpPost]
		public async Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModel([FromBody] ClientAccountFinanceViewModel[] accountPeriods
			, [FromQuery]DateTime? expectedDate = null)
		{
			var userId = GetUserId();
			var accountFinanceViewModelList = await _accountFinanceService.GetAccountFinanceViewModelAsync(accountPeriods, userId, expectedDate);
			return accountFinanceViewModelList;
		}

		[Route("finance/excel")]
		[HttpPost]
		public async Task<FileContentResult> GetExcelAccountFinanceViewModel([FromBody] ClientAccountFinanceViewModel[] accountPeriods)
		{
			var userId = GetUserId();
			var accountFinanceViewModelList = await _accountFinanceService.GetAccountFinanceViewModelAsync(accountPeriods, userId);
			var bytes = ExcelFileHelper.GenerateFile(accountFinanceViewModelList.ToList());
			return File(bytes, "application/octet-stream", ExcelFileHelper.GetFileName(accountFinanceViewModelList));
		}

		[Route("finance/summary")]
		[HttpGet]
		public async Task<IEnumerable<BankAccountSummary>> GetAccountFinanceSummaryViewModel()
		{
			var userId = GetUserId();
			var accounts = await _accountFinanceService.GetAccountFinanceSummaryViewModelAsync(userId);
			return accounts;
		}

		[HttpGet]
		[Route("user")]
		public UserAccountsViewModel GetAccountsByUserId()
		{
			var userId = GetUserId();
			var accounts = _accountService.GetAccountsByUserId(userId);
			return accounts;
		}

		[Route("list")]
		[HttpGet]
		public async Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsViewModel()
		{
			var userId = GetUserId();
			return await _accountService.GetAccountDetailsPeriodViewModelAsync(userId, DateTime.UtcNow);
		}

		[Route("{accountGroupId}")]
		[HttpGet]
		public async Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(int? accountGroupId = null)
		{
			var userId = GetUserId();
			var result = await _accountService.GetAccountDetailsViewModelAsync(userId, accountGroupId);
			return result;
		}

		[HttpGet]
		public IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsInfoViewModel([FromQuery] int[] accountIds)
		{
			var userId = GetUserId();
			var result = _accountService.GetAccountDetailsViewModel(accountIds, userId);
			return result;
		}

		[Route("include/{currencyId}")]
		[HttpGet]
		public IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel([FromRoute]int currencyId, [FromQuery]int? financialEntityId = null)
		{
			var userId = GetUserId();
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
			var userId = GetUserId();
			var result = await _accountService.GetAddAccountViewModelAsync(userId);
			return result;
		}

		[Route("positions")]
		[HttpPut]
		public async Task<IEnumerable<ItemModified>> UpdateAccountPositions([FromBody] IEnumerable<ClientAccountPosition> accountPositions)
		{
			var userId = GetUserId();
			var result = await _accountService.UpdateAccountPositionsAsync(userId, accountPositions);
			return result;
		}

		[HttpPatch]
		public async Task UpdateAccount([FromBody] ClientEditAccount clientEditAccount)
		{
			var userId = GetUserId();
			await _accountService.UpdateAccountAsync(userId, clientEditAccount);
		}

		#endregion
	}
}
