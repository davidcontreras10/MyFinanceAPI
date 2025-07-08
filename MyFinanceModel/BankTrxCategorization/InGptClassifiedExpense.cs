namespace MyFinanceModel.BankTrxCategorization
{
	public class InGptClassifiedExpense
	{
		public string Description { get; set; }
		public string Category { get; set; }
		public string CategoryConfidence { get; set; }
		public string AccountName { get; set; }
		public string AccountConfidence { get; set; }
		public decimal Amount { get; set; }
		public string Currency { get; set; }
	}
}
