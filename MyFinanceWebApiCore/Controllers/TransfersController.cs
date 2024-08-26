using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceWebApiCore.Authentication;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TransfersController(ITransferService transferService, ITransfersMigrationService transfersMigrationService) : BaseApiController
	{

		#region Constructors

		#endregion

		#region Action Methods

		[AdminRequired]
		[HttpPost("migration")]
		public async Task ExecuteMigration()
		{
			await transfersMigrationService.MigrateTransfersAsync();
		}

		[Route("possibleCurrencies")]
		[HttpGet]
		public async Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrencies(int accountId)
		{
			var userId = GetUserId();
			return await transferService.GetPossibleCurrenciesAsync(accountId, userId);
		}

		[Route("possibleDestination")]
		[HttpGet]
		public async Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccount(int accountPeriodId, int currencyId, BalanceTypes balanceType)
		{
			var userId = GetUserId();
			return await transferService.GetPossibleDestinationAccountAsync(accountPeriodId, currencyId, userId,
				balanceType);
		}

		[Route("basicAccountInfo")]
		[HttpGet]
		public async Task<TransferAccountDataViewModel> GetBasicAccountInfo(int accountPeriodId)
		{
			var userId = GetUserId();
			return await transferService.GetBasicAccountInfoAsync(accountPeriodId, userId);
		}

		[HttpPost]
		public async Task<IEnumerable<ItemModified>> CreateTransfer(TransferClientViewModel transferClientViewModel)
		{
			transferClientViewModel.UserId = GetUserId();
			return await transferService.SubmitTransferAsync(transferClientViewModel);
		}

		#endregion
	}
}
