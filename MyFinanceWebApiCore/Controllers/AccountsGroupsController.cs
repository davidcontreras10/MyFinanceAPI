using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AccountsGroupsController : BaseApiController
	{
		#region Attributes

		private readonly IAccountGroupService _accountGroupService;

		#endregion

		#region Constructor

		public AccountsGroupsController(IAccountGroupService accountGroupService)
		{
			_accountGroupService = accountGroupService;
		}

		#endregion

		#region Routes

		[HttpDelete]
		public HttpResponseMessage DeleteAccountGroup(int accountGroupId)
		{
			var userId = GetUserId();
			_accountGroupService.DeleteAccountGroup(userId, accountGroupId);
			return new HttpResponseMessage(HttpStatusCode.OK);
		}

		[HttpPost]
		public int AddAccountGroup(AccountGroupClientViewModel accountGroupViewModel)
		{
			accountGroupViewModel.AccountGroupId = 0;
			var result = _accountGroupService.AddorEditAccountGroup(accountGroupViewModel);
			if (string.IsNullOrEmpty(accountGroupViewModel.UserId))
			{
				accountGroupViewModel.UserId = GetUserId();
			}

			return result;
		}

		[HttpPatch]
		public int EditccountGroup(AccountGroupClientViewModel accountGroupViewModel)
		{
			var result = _accountGroupService.AddorEditAccountGroup(accountGroupViewModel);
			if (string.IsNullOrEmpty(accountGroupViewModel.UserId))
			{
				accountGroupViewModel.UserId = GetUserId();
			}

			return result;
		}

		[Route("{accountGroupId}")]
		[HttpGet]
		public AccountGroupDetailViewModel GetAccountGroupDetailViewModel(int accountGroupId)
		{
			var userId = GetUserId();
			var results = _accountGroupService.GetAccountGroupDetailViewModel(userId, new[] { accountGroupId });
			var result = results.FirstOrDefault(i => i.AccountGroupId == accountGroupId);
			return result;
		}

		[Route("positions")]
		[HttpGet]
		public IEnumerable<AccountGroupPosition> GetAccountGroupPositions(bool validateAdd = false,
			int accountGroupIdSelected = 0)
		{
			var userId = GetUserId();
			var results = _accountGroupService.GetAccountGroupPositions(userId, validateAdd, accountGroupIdSelected);
			return results;
		}

		[HttpGet]
		public IEnumerable<AccountGroupViewModel> GetAccountGroupViewModel()
		{
			var userId = GetUserId();
			var result = _accountGroupService.GetAccountGroupViewModel(userId);
			return result;
		}

		#endregion
	}
}
