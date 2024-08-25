using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;

namespace EFDataAccess.Models
{
	public class EFBankTransaction
	{
		public EFBankTransaction()
		{
			Transactions = new HashSet<SpendOnPeriod>();
		}

		public string BankTransactionId { get; set; }
		public int FinancialEntityId { get; set; }
		public decimal? OriginalAmount { get; set; }
		public int? CurrencyId { get; set; }
		public DateTime? TransactionDate { get; set; }
		public BankTransactionStatus Status { get; set; }
        public string FileDescription { get; set; }

        public virtual FinancialEntity FinancialEntity { get; set; }
		public virtual Currency Currency { get; set; }
		public virtual ICollection<SpendOnPeriod> Transactions { get; set; }
	}
}
