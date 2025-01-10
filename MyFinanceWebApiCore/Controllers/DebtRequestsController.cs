using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ViewModel;
using System;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DebtRequestsController(IDebtRequestService debtRequestService) : BaseApiController
    {
		[HttpGet("add")]
		public async Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestModelAsync()
		{
			var userId = new Guid(GetUserId());
			return await debtRequestService.GetCreateSimpleDebtRequestVmAsync(userId);
		}

	}
}
