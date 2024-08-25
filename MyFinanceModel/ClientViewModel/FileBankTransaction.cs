using System;

namespace MyFinanceModel.ClientViewModel
{
	public class FileBankTransaction
	{
		public string TransactionId { get; set; }	
		public decimal OriginalAmount { get; set; }
		public DateTime? TransactionDate { get; set; }
		public string Description { get; set; }
        public string CurrencyCode { get; set; }
	}
}
