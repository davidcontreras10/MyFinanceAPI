namespace MyFinanceModel.BankTrxCategorization
{
	public interface IGptCacheKey : IWithDescription
	{
		decimal Amount { get; set; }
		string Currency { get; set; }
	}
}
