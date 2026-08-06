namespace MyFinanceModel.ClientViewModel
{
	public class NewDebtRequestAppTrx
	{
		public required float Amount { get; init; }
		public required int AccountId { get; init; }
		public required string Description { get; init; }
		public required int TrxTypeId { get; init; }
	}
}
