using System.Collections.Generic;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Linq;
using MyFinanceBackend.Data;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class TransferService(ISpendsService spendsService,
		ITrxExchangeService trxExchangeService,
		ISpendTypeRepository spendTypeRepository,
		IAccountRepository accountRepository,
		ITransferRepository transferRepository,
		ISpendsRepository spendsRepository,
		IAppTransactionsSubService appTransactionsSubService,
		IUnitOfWork unitOfWork) : ITransferService
	{
		#region Public methods

		#region Get

		public async Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccountAsync(int accountPeriodId, int currencyId,
			string userId, BalanceTypes balanceType)
		{
			if (balanceType != BalanceTypes.Custom)
			{
				var accountInfo = await spendsRepository.GetAccountFinanceViewModelAsync(accountPeriodId, userId);
				currencyId = accountInfo.CurrencyId;
			}

			var accounts = await transferRepository.GetPossibleDestinationAccountAsync(accountPeriodId, currencyId, userId);
			accounts = await GetOrderAccountViewModelsAsync(accounts, userId);
			return accounts;
		}

		public async Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrenciesAsync(int accountId, string userId)
		{
			if (accountId == 0)
				throw new ArgumentException(@"Value cannot be zero", nameof(accountId));

			if (string.IsNullOrEmpty(userId))
				throw new ArgumentNullException(nameof(userId));

			var result = await spendsRepository.GetPossibleCurrenciesAsync(accountId, userId);
			return result;
		}

		public async Task<TransferAccountDataViewModel> GetBasicAccountInfoAsync(int accountPeriodId, string userId)
		{
			var accountInfo = await spendsRepository.GetAccountFinanceViewModelAsync(accountPeriodId, userId);
			if (accountInfo == null)
			{
				return null;
			}

			var accountId = accountInfo.AccountId;
			var currencies = await GetPossibleCurrenciesAsync(accountId, userId);
			var currencyId = currencies.First(c => c.IsDefault).CurrencyId;
			var accounts = await transferRepository.GetPossibleDestinationAccountAsync(accountPeriodId, currencyId, userId);
			accounts = await GetOrderAccountViewModelsAsync(accounts, userId);
			var spendTypes = await spendTypeRepository.GetSpendTypeByAccountViewModelsAsync(userId, accountId);
			var transferData = CreateAccountFinanceViewModel(accountInfo, currencies, accounts, spendTypes);
			return transferData;
		}

		#endregion

		#region Post

		public async Task<IEnumerable<ItemModified>> SubmitTransferAsync(TransferClientViewModel transferClientViewModel)
		{
			ArgumentNullException.ThrowIfNull(transferClientViewModel);

			if (transferClientViewModel.BalanceType == BalanceTypes.Invalid)
			{
				throw new Exception("Invalid balance type");
			}

			await SetTransferClientViewModelAmountAsync(transferClientViewModel);
			if (transferClientViewModel.Amount <= 0)
			{
				throw new Exception("Invalid transfer amount");
			}

			var response = await CreateTransferSpendsResponseAsync(transferClientViewModel);
			var itemsModified = new List<SpendItemModified>();
			try
			{
				transferRepository.BeginTransaction();
				var transactions = new List<ClientAddSpendModel> { response.SpendModel, response.IncomeModel };
				var items = await appTransactionsSubService.AddMultipleTransactionsAsync(transactions);
				var sourceTrxId = items.Where(i => i.RequestId == response.SpendModel.RequestId).Select(i => i.SpendId).FirstOrDefault();
				var destinationTrxId = items.Where(i => i.RequestId == response.IncomeModel.RequestId).Select(i => i.SpendId).FirstOrDefault();
				await unitOfWork.AppTransferRepository.AddAsync(sourceTrxId, destinationTrxId);
				itemsModified.AddRange(items.Select(SpendItemModified.To));
				transferRepository.Commit();
			}
			catch (Exception)
			{
				transferRepository.RollbackTransaction();
				throw;
			}

			return itemsModified;
		}

		#endregion

		#endregion

		#region Privates

		private async Task<IEnumerable<AccountViewModel>> GetOrderAccountViewModelsAsync(IEnumerable<AccountViewModel> accountViewModels,
			string userId)
		{
			if (accountViewModels == null || !accountViewModels.Any())
			{
				return [];
			}

			IEnumerable<AccountViewModel> accountsList = accountViewModels.ToList();
			var orderedAccounts = await
				accountRepository.GetOrderedAccountViewModelListAsync(accountViewModels.Select(acc => acc.AccountId), userId);
			foreach (var orderedAccount in orderedAccounts)
			{
				var account = accountsList.FirstOrDefault(acc => acc.AccountId == orderedAccount.AccountId);
				if (account != null)
				{
					account.GlobalOrder = orderedAccount.GlobalOrder;
				}
			}

			accountsList = accountsList.OrderBy(acc => acc.GlobalOrder);
			return accountsList;
		}

		private async Task<TransferSpendsResponse> CreateTransferSpendsResponseAsync(TransferClientViewModel transferClientViewModel)
		{
			ArgumentNullException.ThrowIfNull(transferClientViewModel);

			var spend = await CreateTransferSpendClientAddSpendModelAsync(transferClientViewModel);
			var accounts = accountRepository.GetAccountPeriodBasicInfo([transferClientViewModel.AccountPeriodId]);
			if(accounts == null || !accounts.Any())
			{
				throw new Exception($"Not account found for account period {transferClientViewModel.AccountPeriodId}");
			}

			var income = await CreateTransferIncomeClientAddSpendModelAsync(transferClientViewModel, accounts.First().AccountId);
			return new TransferSpendsResponse
			{
				SpendModel = spend,
				IncomeModel = income
			};
		}
			
		private async Task<ClientAddSpendModel> CreateTransferIncomeClientAddSpendModelAsync(
			TransferClientViewModel transferClientViewModel, int originalAccountId)
		{
			ArgumentNullException.ThrowIfNull(transferClientViewModel);

			var destinationAccountInfo =(await spendsService.GetAccountsCurrencyAsync(new[] { transferClientViewModel.DestinationAccount }))
					.First(a => a.AccountId == transferClientViewModel.DestinationAccount);
			var destinationCurrencyConverterMethodId = await transferRepository.GetDefaultCurrencyConvertionMethodsAsync(originalAccountId,
				transferClientViewModel.CurrencyId, destinationAccountInfo.CurrencyId,
				transferClientViewModel.UserId);
			var currencyConversionResult = await trxExchangeService.GetExchangeRateResultAsync(destinationCurrencyConverterMethodId,
				transferClientViewModel.SpendDate, true, destinationAccountInfo.CurrencyId, transferClientViewModel.CurrencyId);
			if (currencyConversionResult == null ||
				currencyConversionResult.ResultTypeValue != ExchangeRateResult.ResultType.Success)
				throw new Exception("Invalid currency conversion method result.");
			var accountCurrencyInfo = await spendsRepository.GetAccountMethodConversionInfoAsync(destinationAccountInfo.AccountId, null,
				transferClientViewModel.UserId, destinationAccountInfo.CurrencyId);
			var includeAccountData = accountCurrencyInfo.Where(a => a.AccountId != destinationAccountInfo.AccountId);
			var originalAccountData = accountCurrencyInfo.FirstOrDefault(a => a.AccountId == destinationAccountInfo.AccountId);
			return new ClientAddSpendModel
			{
				SpendTypeId = transferClientViewModel.SpendTypeId,
				Description = transferClientViewModel.Description,
				Amount = transferClientViewModel.Amount,
				UserId = transferClientViewModel.UserId,
				AmountDenominator = (float) currencyConversionResult.Denominator,
				AmountNumerator = (float) currencyConversionResult.Numerator,
				CurrencyId = destinationAccountInfo.CurrencyId,
				IncludedAccounts = includeAccountData,
				OriginalAccountData = originalAccountData,
				SpendDate = transferClientViewModel.SpendDate,
				IsPending = transferClientViewModel.IsPending,
				RequestId = Guid.NewGuid(),
				AmountTypeId = TransactionTypeIds.Saving,
				IsPurchase = currencyConversionResult.IsPurchase,
				MethodId = currencyConversionResult.MethodId
			};
		}

		private async Task<ClientAddSpendModel> CreateTransferSpendClientAddSpendModelAsync(TransferClientViewModel transferClientViewModel)
		{
			var clientAddSpendModel = await
				spendsRepository.CreateClientAddSpendModelAsync(transferClientViewModel, transferClientViewModel.AccountPeriodId);
			clientAddSpendModel.RequestId = Guid.NewGuid();
			clientAddSpendModel.AmountTypeId = TransactionTypeIds.Spend;
			return clientAddSpendModel;
		}

		private static TransferAccountDataViewModel CreateAccountFinanceViewModel(
			AccountFinanceViewModel accountFinanceViewModel, IEnumerable<CurrencyViewModel> currencyViewModels,
			IEnumerable<AccountViewModel> accountViewModels, IEnumerable<SpendTypeViewModel> spendTypeViewModels)
		{
			ArgumentNullException.ThrowIfNull(accountFinanceViewModel);

			var transferViewModel = new TransferAccountDataViewModel
			{
				AccountId = accountFinanceViewModel.AccountId,
				AccountName = accountFinanceViewModel.AccountName,
				AccountPeriodId = accountFinanceViewModel.AccountPeriodId,
				Budget = accountFinanceViewModel.Budget,
				CurrencyId = accountFinanceViewModel.CurrencyId,
				CurrencySymbol = accountFinanceViewModel.CurrencySymbol,
				EndDate = accountFinanceViewModel.EndDate,
				GeneralBalance = accountFinanceViewModel.GeneralBalance,
				GeneralBalanceToday = accountFinanceViewModel.GeneralBalanceToday,
				InitialDate = accountFinanceViewModel.InitialDate,
				PeriodBalance = accountFinanceViewModel.PeriodBalance,
				SpendViewModels = accountFinanceViewModel.SpendViewModels,
				Spent = accountFinanceViewModel.Spent,
				SupportedCurrencies = currencyViewModels,
				SupportedAccounts = accountViewModels,
				SpendTypeViewModels = spendTypeViewModels,
			};

			return transferViewModel;
		}

		private async Task SetTransferClientViewModelAmountAsync(TransferClientViewModel transferClientViewModel)
		{
			ArgumentNullException.ThrowIfNull(transferClientViewModel);

			if (transferClientViewModel.BalanceType == BalanceTypes.Custom)
			{
				return;
			}

			var accountInfo = await GetBasicAccountInfoAsync(transferClientViewModel.AccountPeriodId,
				transferClientViewModel.UserId);
			if (accountInfo == null)
			{
				throw new DataNotFoundException();
			}

			transferClientViewModel.CurrencyId = accountInfo.CurrencyId;
			transferClientViewModel.Amount = transferClientViewModel.BalanceType == BalanceTypes.AccountPeriodBalance
				? accountInfo.PeriodBalance
				: accountInfo.GeneralBalance;
		}

		#endregion
	}

	internal class TransferSpendsResponse
	{
		public ClientAddSpendModel SpendModel { get; set; }
		public ClientAddSpendModel IncomeModel { get; set; }
	}
}