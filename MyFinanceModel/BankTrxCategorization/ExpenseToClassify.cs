namespace MyFinanceModel.BankTrxCategorization
{
	public class ExpenseToClassify
	{
		public string Id { get; set; }
		public string Description { get; set; }
		public decimal Amount { get; set; }
		public string Currency { get; set; }
	}
}
