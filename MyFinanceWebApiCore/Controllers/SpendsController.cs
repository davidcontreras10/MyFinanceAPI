using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;

using System.Collections.Generic;
using System;
using MyFinanceWebApiCore.Authentication;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SpendsController : BaseApiController
	{
		#region Attributes

		private readonly ISpendsService _spendsService;

		#endregion

		#region Constructors

		public SpendsController(ISpendsService spendsService)
		{
			_spendsService = spendsService;
		}

		#endregion

		#region Action Methods

		[Route("actionResult")]
		[RequiresHeaderFilter(ServiceAppHeader.ServiceAppHeaderType.ApplicationModule)]
		[HttpGet]
		public async Task<SpendActionResult> GetSpendActionResult([FromQuery] int spendId, [FromQuery] ResourceActionNames actionType)
		{
			var moduleValue = GetModuleNameValue();
			var result = await _spendsService.GetSpendActionResultAsync(spendId, actionType, moduleValue);
			return result;
		}

		[Route("confirmation")]
		[HttpPut]
		public async Task<IEnumerable<SpendItemModified>> ConfirmPendingSpend([FromQuery] int spendId, [FromBody] DateTimeModel newDateTime)
		{
			if (spendId == 0)
			{
				throw new ArgumentException("Value cannot be zero", nameof(spendId));
			}

			if (newDateTime == null)
			{
				throw new ArgumentNullException(nameof(newDateTime));
			}

			var modifiedItems = await _spendsService.ConfirmPendingSpendAsync(spendId, newDateTime.NewDateTime);
			return modifiedItems;
		}

		[HttpPatch]
		public async Task<IEnumerable<SpendItemModified>> EditSpend([FromQuery] int spendId, [FromBody] ClientEditSpendModel model)
		{
			if(spendId < 0)
			{
				throw new ArgumentException("Value should be greater than 0", nameof(spendId));
			}

			if(model == null)
			{
				throw new ArgumentNullException(nameof(model));
			}

			model.SpendId = spendId;
			model.UserId = GetUserId();
			return await _spendsService.EditSpendAsync(model);
		}

		[Route("basic")]
		[HttpPost]
		public async Task<IEnumerable<ItemModified>> AddBasicSpend([FromBody] ClientBasicTrxByPeriod clientBasicTrxByPeriod)
		{
			clientBasicTrxByPeriod.UserId = GetUserId();
			return await _spendsService.AddBasicTransactionAsync(clientBasicTrxByPeriod, TransactionTypeIds.Spend);
		}

		[Route("basic/income")]
		[HttpPost]
		public async Task<IEnumerable<ItemModified>> AddBasicIncome([FromBody] ClientBasicTrxByPeriod clientBasicTrxByPeriod)
		{
			clientBasicTrxByPeriod.UserId = GetUserId();
			return await _spendsService.AddBasicTransactionAsync(clientBasicTrxByPeriod, TransactionTypeIds.Saving);
		}

		[HttpPost]
		public async Task<IEnumerable<ItemModified>> AddSpendCurrency(ClientAddSpendModel clientAddSpendModel)
		{
			clientAddSpendModel.UserId = GetUserId();
			var result = await _spendsService.AddSpendAsync(clientAddSpendModel);
			return result;
		}

		[Route("income")]
		[HttpPost]
		public async Task<IEnumerable<ItemModified>> AddIncome(ClientAddSpendModel clientAddSpendModel)
		{
			clientAddSpendModel.UserId = GetUserId();
			var result = await _spendsService.AddIncomeAsync(clientAddSpendModel);
			return result;
		}

		[HttpDelete]
		public async Task<IEnumerable<ItemModified>> DeleteSpend(int spendId)
		{
			var userId = GetUserId();
			var itemModifiedList = await _spendsService.DeleteSpendAsync(userId, spendId);
			return itemModifiedList;
		}

		[Route("add")]
		[HttpGet]
		public async Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModel([FromQuery] int[] accountPeriodIds)
		{
			var userId = GetUserId();
			var addSpendViewModelList = await _spendsService.GetAddSpendViewModelAsync(accountPeriodIds, userId);
			return addSpendViewModelList;
		}


		[Route("edit")]
		[HttpGet]
		public async Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModel(int accountPeriodId, int spendId)
		{
			var userId = GetUserId();
			var editSpendViewModelList = await _spendsService.GetEditSpendViewModelAsync(accountPeriodId, spendId,
				userId);
			return editSpendViewModelList;
		}

		#endregion
	}
}
