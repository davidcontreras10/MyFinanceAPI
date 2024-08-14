using MyFinanceBackend.Models;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyFinanceBackend.Data
{
	public static class SpendsDataHelper
	{
		public static IReadOnlyCollection<int> GetInvolvedAccountIds(ISpendCurrencyConvertible currencyConvertible)
		{
			var accountIds = new List<int>() { currencyConvertible.OriginalAccountData.AccountId };
			if(currencyConvertible.IncludedAccounts != null)
			{
				accountIds.AddRange(currencyConvertible.IncludedAccounts.Select(acci => acci.AccountId));
			}

			return accountIds;
		}

		public static ClientAddSpendCurrencyData CreateClientAddSpendCurrencyData(ClientAddSpendAccount clientAddSpendAccount, int amountCurrencyId)
		{
			return new ClientAddSpendCurrencyData
			{
				AmountCurrencyId = amountCurrencyId,
				AccountId = clientAddSpendAccount.AccountId,
				CurrencyConverterMethodId = clientAddSpendAccount.ConvertionMethodId
			};
		}

		public static AccountCurrencyConverterData CreateAccountCurrencyConverterData(
			ClientAddSpendValidationResultSet clientAddSpendValidationResultSet)
		{
			if (clientAddSpendValidationResultSet == null)
				throw new ArgumentNullException(nameof(clientAddSpendValidationResultSet));
			return new AccountCurrencyConverterData
			{
				AccountId = clientAddSpendValidationResultSet.AccountId,
				AccountCurrencyId = clientAddSpendValidationResultSet.AmountCurrencyId,
				AmountCurrencyId = clientAddSpendValidationResultSet.AmountCurrencyId,
				AccountName = clientAddSpendValidationResultSet.AccountName
			};
		}

		public static void ValidateAmountType(ClientBasicAddSpend clientAddSpendModel, bool acceptDefault)
		{
			if (clientAddSpendModel.AmountTypeId == TransactionTypeIds.Invalid)
			{
				if (acceptDefault)
				{
					return;
				}
				throw new InvalidSpendAmountType();
			}
		}
	}
}
