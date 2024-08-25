using System;
using System.Collections.Generic;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientBankTransaction
	{
        public int FinancialEntityId { get; set; }
        public string TransactionId { get; set; }
		public bool RequestIgnore { get; set; }
        public bool SingleTransaction { get; set; }
        public IReadOnlyCollection<TrxAmount> Amounts { get; set; }
		public int? AccountId { get; set; }

		public class TrxAmount
        {
            public decimal Amount { get; set; }
            public int? TrxTypeId { get; set; }
            public string Description { get; set; }
        }
    }
}
