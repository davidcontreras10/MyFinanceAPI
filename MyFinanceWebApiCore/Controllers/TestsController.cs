using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.GptClassifiedExpenseCache;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestsController : BaseApiController
	{
		private readonly IAccountRepository _accountRepository;
		private readonly ISpendsRepository _spendsRepository;
		private readonly IClassifiedExpensesCacheService _classifiedExpensesCacheService;
		private readonly IExpensesClassificationSubService _expensesClassificationSubService;

		public TestsController(IAccountRepository accountRepository
			, ISpendsRepository spendsRepository
			, IClassifiedExpensesCacheService classifiedExpensesCacheService
			, IExpensesClassificationSubService expensesClassificationSubService)
		{
			_accountRepository = accountRepository;
			_spendsRepository = spendsRepository;
			_classifiedExpensesCacheService = classifiedExpensesCacheService;
			_expensesClassificationSubService = expensesClassificationSubService;
		}

		[HttpGet("classify-expenses")]
		public async Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExpenses()
		{
			var userId = GetUserId();
			var trxIds = new[]
			{
				"1256607295","413302","428337","1255903010","620539","922680","530532","117906","1247602175","838134"
			};
			return await _expensesClassificationSubService.ClassifyExistingBankTransactionsAsync(trxIds, 6, userId);
		}

		[HttpGet("historical-expenses-classification")]
		public async Task<IEnumerable<ClassifiedBankTrx>> GetHistoricalExpensesClassificationAsync()
		{
			var userId = GetUserId();
			return await _expensesClassificationSubService.GetClassifiedBankTransactionsAsync(userId);
		}

		[HttpGet("classified-expenses-cache")]
		public async Task<IEnumerable<OutGptClassifiedExpenseCache>> GetClassifiedExpensesCacheAsync()
		{
			return await _classifiedExpensesCacheService.GetItems();
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
