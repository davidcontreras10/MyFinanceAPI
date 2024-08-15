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
		Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientAddSpendModel> clientAddSpendModels);
		Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions);
	}

	public class AppTransactionsSubService(IUnitOfWork unitOfWork, ITrxExchangeService trxExchangeService) : IAppTransactionsSubService
	{
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
				var conversionResult = conversionResults.FirstOrDefault(x => x.Guid == convertibleItem.Guid);
				if (conversionResult == null) throw new Exception("Conversion result not found");
				convertedTrxModels.Add(CreateClientConvertedTrxModel(convertibleItem.SpendCurrencyConvertible as ClientAddSpendModel, conversionResult.DbValues));
			}

			return await unitOfWork.SpendsRepository.AddMultipleTransactionsAsync(convertedTrxModels);
		}

		private async Task<IEnumerable<AccountCurrencyPair>> GetConvertedAccountIncludedAsync(IReadOnlyCollection<ISpendCurrencyConvertible> spendCurrencyConvertibles)
		{
			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(spendCurrencyConvertibles);
			return await unitOfWork.SpendsRepository.GetAccountsCurrencyAsync(accountIds);
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
				TrxTypeId = clientAddSpendModel.SpendTypeId
			};
		}
	}
}
