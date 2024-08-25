using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IAppTransactionsSubService
	{
		Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTrxsByAccountAsync(IReadOnlyCollection<NewAppTransactionByAccount> newAppTransactionsByAccount);
		Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientAddSpendModel> clientAddSpendModels);
		Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions);
	}

	public class AppTransactionsSubService(IUnitOfWork unitOfWork, ITrxExchangeService trxExchangeService) : IAppTransactionsSubService
	{
		public async Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTrxsByAccountAsync(IReadOnlyCollection<NewAppTransactionByAccount> newAppTransactionsByAccount)
		{
			if(newAppTransactionsByAccount == null || newAppTransactionsByAccount.Count == 0) return [];
			var idDates = newAppTransactionsByAccount.Select(x => new IdDateTime(x.AccountId, x.SpendDate)).Distinct().ToList();
			var accountPeriodsInfo = await unitOfWork.AccountRepository.GetAccountPeriodInfoByAccountIdDateTimeAsync(idDates);
			var trxItems = new List<ClientAddSpendModel>();
			foreach (var newAppTransactionByAccount in newAppTransactionsByAccount)
			{
				var idDate = new IdDateTime(newAppTransactionByAccount.AccountId, newAppTransactionByAccount.SpendDate);
				var accountPeriodInfo = accountPeriodsInfo.FirstOrDefault(x => x.Item1 == idDate)?.Item2
					?? throw new ServiceException($"Account period info not found for account {newAppTransactionByAccount.AccountId} for {newAppTransactionByAccount.SpendDate:yyyy/MM/dd}");
				var clientBasicTrxByPeriod = ToClientBasicTrxByPeriod(newAppTransactionByAccount, accountPeriodInfo.AccountPeriodId);
				var clientAddSpendModel = await unitOfWork.SpendsRepository.CreateClientAddSpendModelAsync(clientBasicTrxByPeriod,
						clientBasicTrxByPeriod.AccountPeriodId);
				trxItems.Add(clientAddSpendModel);
			}

			return await AddMultipleTransactionsAsync(trxItems);

		}

		public async Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions)
		{
			return await unitOfWork.SpendsRepository.AddMultipleTransactionsAsync(transactions);
		}

		public async Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientAddSpendModel> clientAddSpendModels)
		{
			if (clientAddSpendModels == null || clientAddSpendModels.Count == 0) return [];
			var convertibleItems = clientAddSpendModels.Select(x => new SpendCurrencyConvertibleItem(Guid.NewGuid(), x)).ToList();
			var accountCurrencyPairs = await GetConvertedAccountIncludedAsync(clientAddSpendModels);
			var conversionResults = await trxExchangeService.ConvertTrxCurrencyAsync(convertibleItems, accountCurrencyPairs.ToList());
			var convertedTrxModels = new List<ClientConvertedTrxModel>();
			foreach (var convertibleItem in convertibleItems)
			{
				var conversionResult = conversionResults.FirstOrDefault(x => x.Guid == convertibleItem.Guid) ?? throw new Exception("Conversion result not found");
				convertedTrxModels.Add(CreateClientConvertedTrxModel(convertibleItem.SpendCurrencyConvertible as ClientAddSpendModel, conversionResult.DbValues));
			}

			return await unitOfWork.SpendsRepository.AddMultipleTransactionsAsync(convertedTrxModels);
		}

		private async Task<IEnumerable<AccountCurrencyPair>> GetConvertedAccountIncludedAsync(IReadOnlyCollection<ISpendCurrencyConvertible> spendCurrencyConvertibles)
		{
			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(spendCurrencyConvertibles);
			return await unitOfWork.SpendsRepository.GetAccountsCurrencyAsync(accountIds);
		}

		private static ClientBasicTrxByPeriod ToClientBasicTrxByPeriod(NewAppTransactionByAccount newAppTransactionByAccount, int accountPeriodId)
		{
			var clientAddSpendModel = new ClientBasicTrxByPeriod
			{
				Amount = newAppTransactionByAccount.Amount,
				AmountTypeId = newAppTransactionByAccount.TransactionType,
				CurrencyId = newAppTransactionByAccount.CurrencyId,
				SpendDate = newAppTransactionByAccount.SpendDate,
				UserId = newAppTransactionByAccount.UserId,
				AccountPeriodId = accountPeriodId,
				Description = newAppTransactionByAccount.Description,
				IsPending = newAppTransactionByAccount.IsPending,
				SpendTypeId = newAppTransactionByAccount.SpendTypeId,
				RequestId = newAppTransactionByAccount.RequestId
			};

			return clientAddSpendModel;
		}

		private static ClientConvertedTrxModel CreateClientConvertedTrxModel(ClientAddSpendModel clientAddSpendModel, IReadOnlyCollection<AddSpendAccountDbValues> dbValues)
		{
			return new ClientConvertedTrxModel
			{
				AmountDenominator = clientAddSpendModel.AmountDenominator,
				AmountNumerator = clientAddSpendModel.AmountNumerator,
				AmountTypeId = clientAddSpendModel.AmountTypeId,
				CurrencyId = clientAddSpendModel.CurrencyId,
				Description = clientAddSpendModel.Description,
				IsPending = clientAddSpendModel.IsPending,
				OriginalAmount = clientAddSpendModel.Amount,
				PeriodTransactions = dbValues,
				TrxDate = clientAddSpendModel.SpendDate,
				TrxTypeId = clientAddSpendModel.SpendTypeId,
				RequestId = clientAddSpendModel.RequestId
			};
		}
	}
}
