using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Models.Requests;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DebtRequestsController(IDebtRequestService debtRequestService) : BaseApiController
    {
		[HttpGet]
		public async Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestsByUserAsync()
		{
			var userId = new Guid(GetUserId());
			return await debtRequestService.GetDebtRequestByUserIdAsync(userId);
		}

		[HttpGet("add")]
		public async Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestModelAsync()
		{
			var userId = new Guid(GetUserId());
			return await debtRequestService.GetCreateSimpleDebtRequestVmAsync(userId);
		}

		[HttpPut("{debtRequestId}/creditor/status")]
		public async Task<ActionResult<UserDebtRequestVm>> UpdateDebtRequestStatusAsync(int debtRequestId, [FromBody] UpdateCreditorStatusReq request)
		{
			var debRequest = await debtRequestService.UpdateCreditorStatusAsync(debtRequestId, request.Status);
			return Ok(debRequest);
		}

		[HttpPut("{debtRequestId}/debtor/status")]
		public async Task<ActionResult<UserDebtRequestVm>> UpdateDebtRequestStatusAsync(int debtRequestId, [FromBody] UpdateDebtorStatusReq request)
		{
			var debRequest = await debtRequestService.UpdateDebtorStatusAsync(debtRequestId, request.Status);
			return Ok(debRequest);
		}


		[HttpPost]
		public async Task<UserDebtRequestVm> CreateDebtRequest([FromBody] NewSimpleDebtRequest newDebtRequest)
		{
			var userId = new Guid(GetUserId());
			var debtRequest = new ClientDebtRequest
			{
				EventName = newDebtRequest.EventName,
				EventDescription = newDebtRequest.EventDescription,
				EventDate = newDebtRequest.EventDate,
				Amount = newDebtRequest.Amount,
				CurrencyId = newDebtRequest.CurrencyId,
				CreditorId = userId,
				DebtorId = newDebtRequest.TargetUserId
			};
			return await debtRequestService.CreateSimpleDebtRequestAsync(debtRequest);
		}

		[HttpDelete("{debtRequestId}")]
		public async Task<ActionResult> DeleteDebtRequestAsync(int debtRequestId)
		{
			var userId = new Guid(GetUserId());
			await debtRequestService.DeleteDebtRequestAsync(debtRequestId);
			return Ok();
		}
	}
}
