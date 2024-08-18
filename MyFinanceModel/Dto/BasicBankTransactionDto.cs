using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;

namespace MyFinanceModel.Dto
{
	public class BasicBankTransactionDto
	{
		public BankTrxId Id => new(FinancialEntityId, BankTransactionId);
		public string BankTransactionId { get; set; }
		public int FinancialEntityId { get; set; }
		public decimal? OriginalAmount { get; set; }
		public int? CurrencyId { get; set; }
		public DateTime? TransactionDate { get; set; }
		public BankTransactionStatus Status { get; set; }
        public IReadOnlyCollection<SpendViewModel> Transactions { get; set; }
        public string Description { get; set; }
    }
}
