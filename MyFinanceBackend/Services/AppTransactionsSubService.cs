using MyFinanceBackend.Data;
using MyFinanceBackend.ServicesExceptions;
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
		Task<IEnumerable<SpendItemModified>> ExecuteConfirmPendingTransactionAsync(int spendId, DateTime newPaymentDate);
		Task<IEnumerable<SpendItemModified>> ExecuteConfirmPendingTransactionsAsync(IReadOnlyCollection<int> transactionIds, DateTime newPaymentDate);
	}

	public class AppTransactionsSubService(IUnitOfWork unitOfWork, ITrxExchangeService trxExchangeService) : IAppTransactionsSubService
	{
		public async Task<IEnumerable<SpendItemModified>> ExecuteConfirmPendingTransactionsAsync(IReadOnlyCollection<int> transactionIds, DateTime newPaymentDate)
		{
			if(transactionIds == null || transactionIds.Count == 0) return [];
			var modifiedList = new List<SpendItemModified>();
			foreach (var spendId in transactionIds)
			{
				var modifiedItems = await ExecuteConfirmPendingTransactionAsync(spendId, newPaymentDate);
				modifiedList.AddRange(modifiedItems);
			}
			return modifiedList;
		}

		public async Task<IEnumerable<SpendItemModified>> ExecuteConfirmPendingTransactionAsync(int spendId, DateTime newPaymentDate)
		{
			var spends = await unitOfWork.SpendsRepository.GetSavedSpendsAsync(spendId);
			if (spends == null || !spends.Any())
			{
				return [];
			}
			var modifiedList = new List<SpendItemModified>();
			foreach (var savedSpend in spends)
			{
				if (!savedSpend.IsPending)
				{
					throw new SpendNotPendingException(savedSpend.SpendId);
				}

				if (savedSpend.AmountNumerator > 0 && savedSpend.AmountDenominator > 0 && savedSpend.MethodId > 0 && savedSpend.IsPurchase != null)
				{
					var exchangeResult = await trxExchangeService.GetExchangeRateResultAsync(savedSpend.MethodId.Value, newPaymentDate, savedSpend.IsPurchase.Value);
					if (exchangeResult == null || !exchangeResult.Success)
					{
						throw new Exception("Exchange rate not found");
					}

					savedSpend.AmountDenominator = (float)exchangeResult.Denominator;
					savedSpend.AmountNumerator = (float)exchangeResult.Numerator;
				}
				var financeSpend = CreateFinanceSpend(savedSpend, newPaymentDate);
				var modifiedItems = await unitOfWork.SpendsRepository.EditSpendAsync(financeSpend);
				modifiedList.AddRange(modifiedItems);
			}

			return modifiedList;
		}

		private static FinanceSpend CreateFinanceSpend(SavedSpend savedSpend, DateTime newDateTime)
		{
			ArgumentNullException.ThrowIfNull(savedSpend);
			var result = new FinanceSpend
			{
				SpendId = savedSpend.SpendId,
				Amount = savedSpend.Amount,
				UserId = savedSpend.UserId,
				SpendDate = savedSpend.SpendDate,
				AmountDenominator = savedSpend.AmountDenominator,
				CurrencyId = savedSpend.CurrencyId,
				AmountNumerator = savedSpend.AmountNumerator,
				SetPaymentDate = newDateTime,
				OriginalAccountData = savedSpend.OriginalAccountData,
				IncludedAccounts = savedSpend.IncludedAccounts,
				IsPending = false,
				AmountTypeId = savedSpend.AmountTypeId
			};

			return result;
		}

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
				RequestId = clientAddSpendModel.RequestId,
				IsPurchase = clientAddSpendModel.IsPurchase,
				MethodId = clientAddSpendModel.MethodId
			};
		}
	}
}
