namespace MyFinanceModel.Dto
{
	public class DebtRequestAppTrxDto
	{
		public float Amount { get; set; }
		public int AccountId { get; set; }
		public string Description { get; set; }
		public int TrxTypeId { get; set; }
	}
}
