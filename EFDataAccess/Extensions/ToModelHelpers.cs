using MyFinanceModel.ViewModel;
using System;
using System.Linq;

namespace EFDataAccess.Extensions
{
	internal static class ToModelHelpers
	{
		public static SpendViewModel ToSpendSpendViewModel(this Models.SpendOnPeriod spendOnPeriod)
		{
			var spend = spendOnPeriod.Spend;
			return new SpendViewModel
			{
				AccountId = spendOnPeriod.AccountPeriod.AccountId ?? 0,
				AmountCurrencyId = spend.AmountCurrencyId ?? 0,
				AmountTypeId = spend.AmountTypeId,
				CurrencyName = spend.AmountCurrency?.Name,
				CurrencySymbol = spend.AmountCurrency?.Symbol,
				Denominator = (float)(spendOnPeriod.Denominator ?? 0),
				Description = spend.Description,
				IsPending = spend.IsPending,
				Numerator = (float)(spendOnPeriod.Numerator ?? 0),
				OriginalAmount = (float)spend.GetAmount(false),
				SetPaymentDate = spend.SetPaymentDate,
				SpendDate = spend.SpendDate ?? new DateTime(),
				SpendId = spend.SpendId,
				SpendTypeId = spend.SpendTypeId ?? 0,
				SpendTypeName = spend.SpendType?.Name,
				UtcRecordDate = spend.UtcRecordDate
			};
		}

		public static T ToSpendSpendViewModel<T>(this Models.SpendOnPeriod spendOnPeriod) where T : SpendViewModel, new()
		{
			var spend = spendOnPeriod.Spend;
			var res = new T
			{
				AccountId = spendOnPeriod.AccountPeriod.AccountId ?? 0,
				AmountCurrencyId = spend.AmountCurrencyId ?? 0,
				AmountTypeId = spend.AmountTypeId,
				CurrencyName = spend.AmountCurrency?.Name,
				CurrencySymbol = spend.AmountCurrency?.Symbol,
				Denominator = (float)(spendOnPeriod.Denominator ?? 0),
				Description = spend.Description,
				IsPending = spend.IsPending,
				Numerator = (float)(spendOnPeriod.Numerator ?? 0),
				OriginalAmount = (float)spend.GetAmount(false),
				SetPaymentDate = spend.SetPaymentDate,
				SpendDate = spend.SpendDate ?? new DateTime(),
				SpendId = spend.SpendId,
				SpendTypeId = spend.SpendTypeId ?? 0,
				SpendTypeName = spend.SpendType?.Name,
				UtcRecordDate = spend.UtcRecordDate
			};

			if (res is FinanceSpendViewModel financeSpendViewModel && spend.SpendOnPeriod != null && spend.SpendOnPeriod.Any())
			{
				financeSpendViewModel.HasBankTrx = spend.SpendOnPeriod.Any(x => x.BankTransactionId != null);
			}

			return res;
		}

	}
}
