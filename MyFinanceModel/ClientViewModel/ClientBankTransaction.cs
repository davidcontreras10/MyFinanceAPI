using System;
using System.Collections.Generic;

namespace MyFinanceModel.ClientViewModel
{
	public record ClientBankTransaction
	{
        public string TransactionId { get; set; }
        public int FinancialEntityId { get; set; }
        public int CurrencyId { get; set; }
        public decimal OriginalAmount { get; set; }
		public DateTime? TransactionDate { get; set; }
        public bool RequestIgnore { get; set; }
        public IReadOnlyCollection<decimal> Amounts { get; set; }
        public string Description { get; set; }

        public class TrxAmount
        {
            public decimal Amount { get; set; }
            public int? TrxTypeId { get; set; }
            public string Description { get; set; }
        }
    }
}
