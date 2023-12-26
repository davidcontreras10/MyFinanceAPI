using EFDataAccess.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestsController : BaseApiController
	{
		private readonly IAccountRepository _accountRepository;
		private readonly ISpendsRepository _spendsRepository;

		public TestsController(IAccountRepository accountRepository, ISpendsRepository spendsRepository)
		{
			_accountRepository = accountRepository;
			_spendsRepository = spendsRepository;
		}

		[HttpGet]
		public async Task<IEnumerable<ClientAddSpendAccount>> TestGetEndpoint(
			[FromQuery]string userId, 
			[FromQuery] int? accountId, 
			[FromQuery] int? accountPeriodId, 
			[FromQuery] int currencyI
			)
		{
			return await _spendsRepository.GetAccountMethodConversionInfoAsync(accountId, accountPeriodId, userId, currencyI);
		}

		[HttpPost]
		public async Task<IEnumerable<AccountViewModel>> TestPostEndpoint([FromBody] TestRequest request)
		{
			return await _accountRepository.GetOrderedAccountViewModelListAsync(request.AccountIds, request.UserId);
		}
	}

	public class TestRequest
	{
        public IEnumerable<int> AccountIds { get; set; }
        public string UserId { get; set; }
    }
}
