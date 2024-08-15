using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.WebMethodsModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface ITrxExchangeService
	{
		Task<IEnumerable<SpendCurrencyConvertibleResult>> ConvertTrxCurrencyAsync(IReadOnlyCollection<SpendCurrencyConvertibleItem> spendCurrencyConvertibleItems,
			IReadOnlyCollection<AccountCurrencyPair> accountCurrencyPairList);
		Task<IEnumerable<AddSpendAccountDbValues>> ConvertTrxCurrencyAsync(ISpendCurrencyConvertible spendCurrencyConvertible, IReadOnlyCollection<AccountCurrencyPair> accountCurrencyPairList);
		Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isIncome, int accountCurrencyId, int amountCurrencyId);
	}

	public class TrxExchangeService(ICurrencyService currencyService) : ITrxExchangeService
	{
		private const int CountryCurrencyId = 1;

		public async Task<IEnumerable<SpendCurrencyConvertibleResult>> ConvertTrxCurrencyAsync(IReadOnlyCollection<SpendCurrencyConvertibleItem> spendCurrencyConvertibleItems,
			IReadOnlyCollection<AccountCurrencyPair> accountCurrencyPairList)
		{
			if (spendCurrencyConvertibleItems == null || spendCurrencyConvertibleItems.Count == 0)
			{
				throw new ArgumentException(@"Value cannot be null or empty", nameof(spendCurrencyConvertibleItems));
			}

			var methodParams = new List<MethodParam>();
			foreach (var spendCurrencyConvertibleItem in spendCurrencyConvertibleItems)
			{
				ExtractMethodsParams(spendCurrencyConvertibleItem.SpendCurrencyConvertible, accountCurrencyPairList, methodParams);
			}

			var exchangeResults = await currencyService.GetExchangeRateResultAsync(methodParams);
			var dbValueItems = new List<SpendCurrencyConvertibleResult>();
			foreach (var spendCurrencyConvertibleItem in spendCurrencyConvertibleItems)
			{
				var dbValues = new List<AddSpendAccountDbValues>();
				var spendCurrencyConvertible = spendCurrencyConvertibleItem.SpendCurrencyConvertible;
				var clientAddSpendAccount = new List<ClientAddSpendAccount>
				{
					spendCurrencyConvertible.OriginalAccountData
				};
				clientAddSpendAccount.AddRange(spendCurrencyConvertible.IncludedAccounts);
				var amountCurrency = spendCurrencyConvertible.CurrencyId;
				var accountModelCurrencyList = CreateAccountModelCurrency(spendCurrencyConvertible, accountCurrencyPairList);
				foreach(AccountModelCurrency accountModelCurrency in accountModelCurrencyList)
				{
					if(accountModelCurrency.AccountOriginalCurrencyId == amountCurrency)
					{
						dbValues.Add(CreateAddSpendAccountDbValues(accountModelCurrency, ExchangeRateResult.CreateDefaultExchangeRateResult()));
						continue;
					}

					var accountConvertInfo = accountCurrencyPairList.FirstOrDefault(item => item.AccountId == accountModelCurrency.AccountInfo.AccountId);
					var isPurchase = IsPurchase(spendCurrencyConvertible.AmountTypeId == TransactionTypeIds.Saving, accountConvertInfo.CurrencyId, amountCurrency);
					var exchangeRateResult = exchangeResults.FirstOrDefault(item => item.MethodId == accountModelCurrency.AccountInfo.ConvertionMethodId && item.IsPurchase == isPurchase);
					if (exchangeRateResult == null)
					{
						throw new Exception("Exchange rate not found");
					}

					dbValues.Add(CreateAddSpendAccountDbValues(accountModelCurrency, exchangeRateResult));
				}

				foreach(var dbValue in dbValues)
				{
					dbValue.IsOriginal = dbValue.AccountId == spendCurrencyConvertibleItem.SpendCurrencyConvertible.OriginalAccountData.AccountId;
				}
				dbValueItems.Add(new SpendCurrencyConvertibleResult(spendCurrencyConvertibleItem.Guid, dbValues));

			}

			return dbValueItems;
		}

		public async Task<IEnumerable<AddSpendAccountDbValues>> ConvertTrxCurrencyAsync(
			ISpendCurrencyConvertible spendCurrencyConvertible,
			IReadOnlyCollection<AccountCurrencyPair> accountCurrencyPairList)
		{
			if (spendCurrencyConvertible == null)
				throw new ArgumentNullException(nameof(spendCurrencyConvertible));
			var accountModelCurrencyList = CreateAccountModelCurrency(spendCurrencyConvertible, accountCurrencyPairList);
			var addSpendAccountDbValues = await ConvertTrxCurrencyAsync2(spendCurrencyConvertible.PaymentDate,
																			spendCurrencyConvertible.CurrencyId,
																			accountModelCurrencyList,
																			spendCurrencyConvertible.AmountTypeId == MyFinanceModel.ClientViewModel.TransactionTypeIds.Saving
																			);
			foreach (var value in addSpendAccountDbValues)
			{
				value.IsOriginal = value.AccountId == spendCurrencyConvertible.OriginalAccountData.AccountId;
			}

			return addSpendAccountDbValues;
		}

		public async Task<ExchangeRateResult> GetExchangeRateResultAsync(int methodId, DateTime dateTime, bool isIncome, int accountCurrencyId, int amountCurrencyId)
		{
			var isPurchase = IsPurchase(isIncome, accountCurrencyId, amountCurrencyId);
			return await currencyService.GetExchangeRateResultAsync(methodId, dateTime, isPurchase);
		}


		private static IEnumerable<AccountModelCurrency> CreateAccountModelCurrency(ISpendCurrencyConvertible spendCurrencyConvertible,
																	 IEnumerable<AccountCurrencyPair> pairs)
		{
			if (spendCurrencyConvertible == null)
				throw new ArgumentNullException(nameof(spendCurrencyConvertible));
			if (pairs == null || !pairs.Any())
				throw new ArgumentException(@"Value cannot be null or empty", nameof(pairs));
			var accountModelCurrencyList = new List<AccountModelCurrency>
				{
					new() {
							AccountInfo = spendCurrencyConvertible.OriginalAccountData,
							AccountOriginalCurrencyId =
								pairs.First(item => item.AccountId == spendCurrencyConvertible.OriginalAccountData.AccountId)
									 .CurrencyId
						}
				};
			accountModelCurrencyList.AddRange(
				spendCurrencyConvertible.IncludedAccounts.Select(clientAddSpendAccount => new AccountModelCurrency
				{
					AccountInfo = clientAddSpendAccount,
					AccountOriginalCurrencyId =
							pairs.First(item => item.AccountId == clientAddSpendAccount.AccountId).CurrencyId
				}));
			return accountModelCurrencyList;
		}

		private static void ExtractMethodsParams(ISpendCurrencyConvertible spendCurrencyConvertible,
			IReadOnlyCollection<AccountCurrencyPair> accountCurrencyPairList,
			List<MethodParam> methodParams)
		{
			var clientAddSpendAccount = new List<ClientAddSpendAccount>
			{
				spendCurrencyConvertible.OriginalAccountData
			};
			clientAddSpendAccount.AddRange(spendCurrencyConvertible.IncludedAccounts);
			var amountCurrency = spendCurrencyConvertible.CurrencyId;
			var accountModelCurrencyList = CreateAccountModelCurrency(spendCurrencyConvertible, accountCurrencyPairList);
			foreach (var clientAddAccount in accountModelCurrencyList)
			{
				var accountConvertInfo = accountCurrencyPairList.FirstOrDefault(item => item.AccountId == clientAddAccount.AccountInfo.AccountId);
				if (accountConvertInfo == null)
					throw new Exception("accountCurrencyId not found");
				if (accountConvertInfo.CurrencyId == amountCurrency)
					continue;

				var isPurchase = IsPurchase(spendCurrencyConvertible.AmountTypeId == TransactionTypeIds.Saving, accountConvertInfo.CurrencyId, amountCurrency);
				var methodParam = new MethodParam(clientAddAccount.AccountInfo.ConvertionMethodId, isPurchase, spendCurrencyConvertible.PaymentDate);
				if (!methodParams.Contains(methodParam))
					methodParams.Add(methodParam);
			}
		}

		private async Task<IEnumerable<AddSpendAccountDbValues>> ConvertTrxCurrencyAsync(
			DateTime dateTime,
			int amountCurrency,
			IEnumerable<AccountModelCurrency> accountModelCurrencies,
			bool isIncome
			)
		{
			var dbValues = new List<AddSpendAccountDbValues>();
			dbValues.AddRange(
				accountModelCurrencies.Where(item => item.AccountOriginalCurrencyId == amountCurrency)
									  .Select(
										  item2 =>
										  CreateAddSpendAccountDbValues(item2,
																		ExchangeRateResult
																			.CreateDefaultExchangeRateResult())));
			var methodIds =
				accountModelCurrencies.Where(
					item => dbValues.All(item2 => item2.AccountId != item.AccountInfo.AccountId))
									  .Select(item3 => new ExchangeRateResultModel.MethodParam
									  {
										  Id = item3.AccountInfo.ConvertionMethodId,
										  IsPurchase = IsPurchase(isIncome, item3.AccountOriginalCurrencyId, amountCurrency),
										  DateTime = dateTime
									  });//TODO validate methodId not to be repeated
			if (!methodIds.Any())
				return dbValues;
			var exchangeRateResultList = await currencyService.GetExchangeRateResultAsync(methodIds);
			dbValues.AddRange(
				accountModelCurrencies.Where(
					item => dbValues.All(item2 => item2.AccountId != item.AccountInfo.AccountId))
					.Select(
						item3 =>
							CreateAddSpendAccountDbValues(item3,
								exchangeRateResultList.First(
									item4 =>
										item4.MethodId ==
										item3.AccountInfo.ConvertionMethodId)))
				);
			return dbValues;
		}

		private async Task<IEnumerable<AddSpendAccountDbValues>> ConvertTrxCurrencyAsync2(
			DateTime dateTime,
			int amountCurrency,
			IEnumerable<AccountModelCurrency> accountModelCurrencies,
			bool isIncome
			)
		{
			var dbValues = new List<AddSpendAccountDbValues>();
			dbValues.AddRange(
				accountModelCurrencies.Where(item => item.AccountOriginalCurrencyId == amountCurrency)
									  .Select(
										  item2 =>
										  CreateAddSpendAccountDbValues(item2,
																		ExchangeRateResult
																			.CreateDefaultExchangeRateResult())));
			var methodIds = new List<ExchangeRateResultModel.MethodParam>();
			foreach (var accountModelCurrency in accountModelCurrencies)
			{
				var allMatch = true;
				foreach (var dbValue in dbValues)
				{
					if (dbValue.AccountId == accountModelCurrency.AccountInfo.AccountId)
					{
						allMatch = false;
						break;
					}
				}

				if (allMatch)
				{
					var methodParam = new ExchangeRateResultModel.MethodParam
					{
						Id = accountModelCurrency.AccountInfo.ConvertionMethodId,
						IsPurchase = IsPurchase(isIncome, accountModelCurrency.AccountOriginalCurrencyId, amountCurrency),
						DateTime = dateTime
					};

					methodIds.Add(methodParam);
				}
			}

			if (methodIds.Count == 0)
				return dbValues;
			var exchangeRateResultList = await currencyService.GetExchangeRateResultAsync(methodIds);

			dbValues.AddRange(
				accountModelCurrencies
					.Where(item => dbValues.All(item2 => item2.AccountId != item.AccountInfo.AccountId))
					.Select(item3 =>
						CreateAddSpendAccountDbValues(
							item3,
							exchangeRateResultList.First(item4 => item4.MethodId == item3.AccountInfo.ConvertionMethodId)
						)
					)
			);

			return dbValues;

		}

		private static bool IsPurchase(bool isIncome, int accountCurrencyId, int amountCurrencyId)
		{
			if (accountCurrencyId == amountCurrencyId)
			{
				return false;
			}

			if (accountCurrencyId != CountryCurrencyId && amountCurrencyId != CountryCurrencyId)
			{
				throw new Exception("Unable to convert to a non country currency");
			}

			bool res;
			//dolares account
			if (accountCurrencyId != CountryCurrencyId)
			{
				//isIncome
				res = false;
				if (!isIncome)
				{
					res = true;
				}
			}
			else //colones account
			{
				//isIncome
				res = true;
				if (!isIncome)
				{
					res = false;
				}
			}

			return res;
		}

		private static AddSpendAccountDbValues CreateAddSpendAccountDbValues(AccountModelCurrency accountModelCurrency,
																	 ExchangeRateResult exchangeRateResult)
		{
			if (accountModelCurrency == null)
				throw new ArgumentNullException(nameof(accountModelCurrency));
			if (exchangeRateResult == null || !exchangeRateResult.Success)
				throw new InvalidExchangeRateCreationException("Cannot create value with invalid exchangeRateResult");
			return new AddSpendAccountDbValues
			{
				AccountId = accountModelCurrency.AccountInfo.AccountId,
				PendingUpdate = exchangeRateResult.ResultTypeValue == ExchangeRateResult.ResultType.PendingUpdate,
				Numerator = exchangeRateResult.Numerator,
				Denominator = exchangeRateResult.Denominator,
				CurrencyConverterMethodId = accountModelCurrency.AccountInfo.ConvertionMethodId
			};
		}


	}
}
