using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Models
{
    public class ClientAddSpendCurrencyData
    {
        public int AmountCurrencyId { get; set; }
        public int AccountId { get; set; }
        public int CurrencyConverterMethodId { get; set; }

    }

    internal class AccountModelCurrency
    {
        public ClientAddSpendAccount AccountInfo { get; set; }
        public int AccountOriginalCurrencyId { get; set; }
    }

    public class AccountCurrencyPair
    {
        public int AccountId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class AddSpendDbValues
    {
        public IEnumerable<AddSpendAccountDbValues> IncludedAccounts { get; set; }
        public float Amount { get; set; }
        public float? AmountNumerator { get; set; }
        public float? AmountDenominator { get; set; }
        public DateTime SpendDate { get; set; }
        public int SpendTypeId { get; set; }
        public string UserId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class AddSpendAccountDbValues
    {
        public int AccountId { get; set; }
        public double Numerator { get; set; }
        public double Denominator { get; set; }
        public bool PendingUpdate { get; set; }
        public int CurrencyConverterMethodId { get; set; }
        public bool IsOriginal { get; set; }
    }
}
