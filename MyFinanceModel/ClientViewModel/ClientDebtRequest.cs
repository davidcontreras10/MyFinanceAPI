using System;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientDebtRequest
	{
		public string EventName { get; set; }
		public string EventDescription { get; set; }
		public DateTime EventDate { get; set; }
		public decimal Amount { get; set; }
		public int CurrencyId { get; set; }
		public Guid CreditorId { get; set; }
		public Guid DebtorId { get; set; }
	}
}
