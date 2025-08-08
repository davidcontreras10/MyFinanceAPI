namespace MyFinanceModel.BankTrxCategorization
{
	public interface IExpenseItem : IWithDescription
	{
		string AccountName { get; set; }
		string Category { get; set; }
		decimal Amount { get; set; }
		string Currency { get; set; }
	}
}
