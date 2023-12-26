using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TransfersController : BaseApiController
	{
		#region Attributes

		private readonly ITransferService _transferService;

		#endregion

		#region Constructors

		public TransfersController(ITransferService transferService)
		{
			_transferService = transferService;
		}

		#endregion

		#region Action Methods

		[Route("possibleCurrencies")]
		[HttpGet]
		public async Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrencies(int accountId)
		{
			var userId = GetUserId();
			return await _transferService.GetPossibleCurrenciesAsync(accountId, userId);
		}

		[Route("possibleDestination")]
		[HttpGet]
		public async Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccount(int accountPeriodId, int currencyId, BalanceTypes balanceType)
		{
			var userId = GetUserId();
			return await _transferService.GetPossibleDestinationAccountAsync(accountPeriodId, currencyId, userId,
				balanceType);
		}

		[Route("basicAccountInfo")]
		[HttpGet]
		public async Task<TransferAccountDataViewModel> GetBasicAccountInfo(int accountPeriodId)
		{
			var userId = GetUserId();
			return await _transferService.GetBasicAccountInfoAsync(accountPeriodId, userId);
		}

		[HttpPost]
		public async Task<IEnumerable<ItemModified>> CreateTransfer(TransferClientViewModel transferClientViewModel)
		{
			transferClientViewModel.UserId = GetUserId();
			return await _transferService.SubmitTransferAsync(transferClientViewModel);
		}

		#endregion
	}
}
