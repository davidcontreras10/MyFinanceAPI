using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Models;
using MyFinanceWebApiCore.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountPeriodsController : BaseApiController
	{
		private readonly IAccountFinanceService _accountFinanceService;

		public AccountPeriodsController(IAccountFinanceService accountFinanceService)
		{
			_accountFinanceService = accountFinanceService;
		}

		[HttpGet]
		[Route("{accountPeriodId}/excel")]
		public async Task<FileContentResult> GetAccountPeriodExcelFileAsync([FromRoute] int accountPeriodId, [FromQuery] bool isPending = true)
		{
			var accountPeriods = new ClientAccountFinanceViewModel
			{
				AccountPeriodId = accountPeriodId,
				LoanSpends = false,
				PendingSpends = isPending
			};

			var userId = GetUserId();
			var accountFinanceViewModelList = await _accountFinanceService.GetAccountFinanceViewModelAsync(new[] { accountPeriods }, userId);
			var bytes = ExcelFileHelper.GenerateFile(accountFinanceViewModelList.ToList());
			return File(bytes, "application/octet-stream", ExcelFileHelper.GetFileName(accountFinanceViewModelList));
		}
	}
}
