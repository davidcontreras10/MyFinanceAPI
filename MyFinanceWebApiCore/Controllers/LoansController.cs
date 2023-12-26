using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LoansController : BaseApiController
	{
		#region Attributes

		private readonly ILoanService _loanService;

		#endregion

		#region Constructor

		public LoansController(ILoanService loanService)
		{
			_loanService = loanService;
		}

		#endregion

		#region Api

		[HttpDelete]
		public IEnumerable<ItemModified> DeleteLoan(int loanRecordId)
		{
			var userId = GetUserId();
			var result = _loanService.DeleteLoan(loanRecordId, userId);
			return result;
		}

		[HttpPost]
		public async Task<IEnumerable<SpendItemModified>> CreateLoan(ClientLoanViewModel clientLoanViewModel)
		{
			if (clientLoanViewModel == null)
			{
				throw new ArgumentNullException(nameof(clientLoanViewModel));
			}

			clientLoanViewModel.UserId = GetUserId();
			var response = await _loanService.CreateLoanAsync(clientLoanViewModel);
			return response;
		}

		[Route("payment")]
		[HttpPost]
		public async Task<IEnumerable<SpendItemModified>> AddPayment(ClientLoanSpendViewModel clientLoanSpendViewModel)
		{
			if (clientLoanSpendViewModel == null)
			{
				throw new ArgumentNullException(nameof(clientLoanSpendViewModel));
			}

			clientLoanSpendViewModel.UserId = GetUserId();
			var response = await _loanService.AddLoanSpendAsync(clientLoanSpendViewModel);
			return response;
		}

		[Route("accounts")]
		[HttpGet]
		public async Task<IEnumerable<AccountDetailsViewModel>> GetSupportedLoanAccount()
		{
			var userId = GetUserId();
			var result = await _loanService.GetSupportedLoanAccountAsync(userId);
			return result;
		}

		[Route("add")]
		[HttpGet]
		public async Task<AddLoanRecordViewModel> GetAddLoanRecordViewModel(int accountId, DateTime dateTime)
		{
			if (accountId == 0)
			{
				throw new ArgumentException(nameof(accountId));
			}

			var userId = GetUserId();
			var response = await _loanService.GetAddLoanRecordViewModelAsync(dateTime, accountId, userId);
			return response;
		}

		[Route("add/payment")]
		[HttpGet]
		public async Task<AddLoanSpendViewModel> GetAddLoanSpendViewModel(int loanRecordId)
		{
			if (loanRecordId == 0)
			{
				throw new ArgumentException(nameof(loanRecordId));
			}

			var userId = GetUserId();
			var response = await _loanService.GetAddLoanSpendViewModelAsync(loanRecordId, userId);
			return response;
		}

		[Route("{loanRecordId}")]
		[HttpGet]
		public LoanReportViewModel GetLoanReportViewModelByLoanRecordId(int loanRecordId)
		{
			var response = _loanService.GetLoanDetailRecordsByIds(new[] { loanRecordId });
			return response.FirstOrDefault();
		}

		[HttpGet]
		public IEnumerable<LoanReportViewModel> GetLoanDetailRecordsByCriteriaId([FromQuery] int loanRecordStatusId, [FromQuery] LoanQueryCriteria criteriaId, [FromQuery] int[] ids = null)
		{
			var userId = GetUserId();
			var result = _loanService.GetLoanDetailRecordsByCriteriaId(userId, loanRecordStatusId, criteriaId, ids, ids);
			return result;
		}

		[HttpGet]
		[Route("destinationAccounts")]
		public async Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccount(int accountId, DateTime dateTime,
			int currencyId)
		{
			var userId = GetUserId();
			var result = await _loanService.GetPossibleDestinationAccountAsync(accountId, dateTime, userId, currencyId);
			return result;
		}

		#endregion
	}
}
