using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Dto;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class DebtRequestsController(IDebtRequestService debtRequestService) : BaseApiController
	{
		[HttpDelete("{debtRequestId}/app-trxs")]
		public async Task<ActionResult> RemoveAppTransactionsFromDebtRequestAsync([FromRoute] int debtRequestId)
		{
			var userId = new Guid(GetUserId());
			await debtRequestService.RemoveAppTransactionsFromDebtRequestAsync(debtRequestId, userId);
			return Ok();
		}

		[HttpGet("{debtRequestId}/app-trxs")]
		public async Task<IReadOnlyCollection<DebtRequestAppTrxDto>> GetAppTransactionsByDebtRequestIdAsync([FromRoute] int debtRequestId)
		{
			var userId = new Guid(GetUserId());
			var debtRequest = await debtRequestService.GetDebtRequestByIdAsync(debtRequestId, userId);
			var transactions = debtRequest.UserTrxs.Select(x => new DebtRequestAppTrxDto
			{
				AccountId = x.AccountId,
				Amount = x.OriginalAmount,
				Description = x.Description,
				TrxTypeId = x.SpendTypeId,

			}).ToList();

			return transactions;
		}

		[HttpPost("{debtRequestId}/app-trxs")]
		public async Task<ActionResult<IReadOnlyCollection<TrxItemModifiedRecord>>> CreateAppTransactionAsync([FromRoute] int debtRequestId, [FromBody] IReadOnlyCollection<AppTrxDebtItem> items)
		{
			var userId = new Guid(GetUserId());
			var debtRequestAppTrxs = items.Select(x => new NewDebtRequestAppTrx
			{
				AccountId = x.AccountId,
				Amount = x.Amount,
				Description = x.Description,
				TrxTypeId = x.TrxTypeId
			}).ToList();
			var modifiedItems = await debtRequestService.AddAppTransactionsToDebtRequestAsync(userId, debtRequestId, debtRequestAppTrxs);
			return Ok(modifiedItems);
		}

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
			var userId = new Guid(GetUserId());
			var debRequest = await debtRequestService.UpdateCreditorStatusAsync(debtRequestId, request.Status, userId, request.DateTime);
			return Ok(debRequest);
		}

		[HttpPut("{debtRequestId}/debtor/status")]
		public async Task<ActionResult<UserDebtRequestVm>> UpdateDebtRequestStatusAsync(int debtRequestId, [FromBody] UpdateDebtorStatusReq request)
		{
			var userId = new Guid(GetUserId());
			var debRequest = await debtRequestService.UpdateDebtorStatusAsync(debtRequestId, request.Status, userId, request.DateTime);
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
