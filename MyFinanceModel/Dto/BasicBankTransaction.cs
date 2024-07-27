using MyFinanceModel.Enums;
using System;

namespace MyFinanceModel.Dto
{
	public class BasicBankTransaction
	{
		public string BankTransactionId { get; set; }
		public int FinancialEntityId { get; set; }
		public decimal? OriginalAmount { get; set; }
		public int? CurrencyId { get; set; }
		public DateTime? TransactionDate { get; set; }
		public BankTransactionStatus Status { get; set; }
	}
}
