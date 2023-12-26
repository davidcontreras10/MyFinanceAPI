using System;
using System.Collections.Generic;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceModel
{
    public class FinanceSpend : ISpendCurrencyConvertible
    {
        #region Attributes

        public string UserId { get; set; }

        public int SpendId { get; set; }

        public float Amount { get; set; }

        public float? AmountNumerator { get; set; }

        public float? AmountDenominator { get; set; }

        public DateTime SpendDate { get; set; }

        public DateTime SetPaymentDate { get; set; }

        public ClientAddSpendAccount OriginalAccountData { get; set; }

        public IEnumerable<ClientAddSpendAccount> IncludedAccounts { get; set; }

        public int CurrencyId { get; set; }

        public bool IsPending { get; set; }

        public DateTime PaymentDate => SetPaymentDate;

		public TransactionTypeIds AmountTypeId { get; set; }

		#endregion
	}
}
