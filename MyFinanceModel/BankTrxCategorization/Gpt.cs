namespace MyFinanceModel.BankTrxCategorization
{
	public static class Gpt
	{
		public record Account(string Description, int Id, string Name);
		public record Category(int Id, string Name);
	}
}
