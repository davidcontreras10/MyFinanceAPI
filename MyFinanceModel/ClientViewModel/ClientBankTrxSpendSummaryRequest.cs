using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientBankTrxSpendSummaryRequest
	{
		public IReadOnlyCollection<ClientBankTrxSpendSummaryRequestItem> Transactions { get; set; } = [];
	}

	public class ClientBankTrxSpendSummaryRequestItem
	{
		[Required]
		public string TransactionId { get; set; }

		[Required]
		public int FinancialEntityId { get; set; }
	}
}
