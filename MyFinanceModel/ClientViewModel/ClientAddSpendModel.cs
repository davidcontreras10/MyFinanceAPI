using System;
using System.Collections.Generic;

namespace MyFinanceModel.ClientViewModel
{
    public class ClientAddSpendModel : ClientBasicAddSpend, ISpendCurrencyConvertible
    {
        #region Attributes
		        
		public ClientAddSpendAccount OriginalAccountData { get; set; }
        
		public IEnumerable<ClientAddSpendAccount> IncludedAccounts { get; set; }

        #endregion
    }

    public class ClientEditSpendModel : ClientAddSpendModel
    {
        public int SpendId { set; get; }
        public IEnumerable<Field> ModifyList { set; get; }

        public enum Field
        {
            Invalid = 0,
            //SpendDate = 1,
            //Amount = 2,
            //AccountsInclude = 3,
            Description = 4,
            SpendType = 5,
            //CurrencyId = 6,
            AmountType = 7

		}
    }

    public class ClientAddSpendAccount
    {
        #region Attributes

        public int AccountId { get; set; }
        public int ConvertionMethodId { get; set; }

        #endregion
    }

    public class ClientConvertedTrxModel
    {
        public IEnumerable<AddSpendAccountDbValues> PeriodTransactions { get; set; }
        public int CurrencyId { get; set; }
        public TransactionTypeIds AmountTypeId { get; set; }
        public double? AmountDenominator { get; set; }
        public double? AmountNumerator { get; set; }
        public string Description { get; set; }
        public double OriginalAmount { get; set; }
        public bool IsPending { get; set; }
        public DateTime TrxDate { get; set; }
        public int TrxTypeId { get; set; }
    }
}
