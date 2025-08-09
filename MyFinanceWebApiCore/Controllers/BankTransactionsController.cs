using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceWebApiCore.Models;
using MyFinanceWebApiCore.Models.Requests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BankTransactionsController(IExpensesClassificationSubService expensesClassificationSubService) : BaseApiController
	{
		[HttpPost("ai-classification")]
		public async Task<IReadOnlyCollection<AIClassifiedBankTrx>> ClassifyBankTransactions([FromBody] ClassifyBankTransactionsRequest request)
		{
			var userId = GetUserId();
			var classifiedExpenses = await expensesClassificationSubService.ClassifyExistingBankTransactionsAsync(
				request.BankTrxIds, request.FinancialEntityId, userId);
			return classifiedExpenses.Select(e =>
				new AIClassifiedBankTrx
				{
					AccountId = e.AccountId,
					Id = e.Id,
					TrxTypeId = e.CategoryId
				}).ToList();
		}
	}
}
