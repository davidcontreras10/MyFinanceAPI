using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

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
                SpendTypeName = spend.SpendType?.Name
            };
        }
    }
}
