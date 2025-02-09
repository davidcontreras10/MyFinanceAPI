using System;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class NewSimpleDebtRequest
	{
		public string EventName { get; set; }
		public string EventDescription { get; set; }
		public DateTime EventDate { get; set; }
		public decimal Amount { get; set; }
		public int CurrencyId { get; set; }
		public Guid TargetUserId { get; set; }
	}
}
