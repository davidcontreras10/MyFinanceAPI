using System;

namespace EFDataAccess.Models
{
	public class EFDebtRequest
	{
		public int Id { get; set; }
		public Guid DebtorUserId { get; set; }
		public Guid CreditorUserId { get; set; }
		public bool MarkedAsPaid { get; set; }
		public int? DebtorSpendId { get; set; }
		public int? CreditorSpendId { get; set; }
		public string EventName { get; set; }
		public string EventDescription { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime EventDate { get; set; }
		public decimal Amount { get; set; }
		public int CurrencyId { get; set; }

		public virtual Spend DebtorSpend { get; set; }
		public virtual Spend CreditorSpend { get; set; }
		public virtual Currency Currency { get; set; }
		public virtual AppUser DebtorUser { get; set; }
		public virtual AppUser CreditorUser { get; set; }
	}
}
