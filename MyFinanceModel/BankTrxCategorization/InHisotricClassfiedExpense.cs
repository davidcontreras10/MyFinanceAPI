namespace MyFinanceModel.BankTrxCategorization
{
	public class InHisotricClassfiedExpense : IExpenseItem
	{
		public string Description { get; set; }
		public int CategoryId { get; set; }
		public string Category { get; set; }
		public string CategoryConfidence { get; set; }
		public int AccountId { get; set; }
		public string AccountName { get; set; }
		public string AccountConfidence { get; set; }
		public decimal Amount { get; set; }
		public string Currency { get; set; }
	}
}
