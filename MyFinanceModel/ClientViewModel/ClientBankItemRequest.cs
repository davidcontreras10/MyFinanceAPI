using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientBankItemRequest
	{
		[Required]
		public string TransactionId { get; set; }

		[Required]
		public int FinancialEntityId { get; set; }

        public bool RequestIgnore { get; set; }

        public string Description { get; set; }

        public bool? IsMultipleTrx { get; set; }

        public int? AccountId { get; set; }

        public int? SpendTypeId { get; set; }

        public bool? IsPending { get; set; }

        public IReadOnlyCollection<ClientBankTrxRequest> Transactions { get; set; }
    }

	public class ClientBankTrxRequest
	{
        public decimal Amount { get; set; }
		public bool IsPending { get; set; }
        public int AccountId { get; set; }
        public int SpendTypeId { get; set; }
		public string Description { get; set; }
	}
}
