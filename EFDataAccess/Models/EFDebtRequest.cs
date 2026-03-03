using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;

namespace EFDataAccess.Models
{
	public class EFDebtRequest
	{
		public EFDebtRequest()
		{
			DebtorSpends = [];
			CreditorSpends = [];
		}

		public int Id { get; set; }

		public string EventName { get; set; }
		public string EventDescription { get; set; }
		public DateTime CreatedDate { get; set; }
		public DateTime EventDate { get; set; }
		public decimal Amount { get; set; }
		public int CurrencyId { get; set; }

		public virtual ICollection<Spend> DebtorSpends { get; set; }
		public virtual ICollection<Spend> CreditorSpends { get; set; }
		public virtual Currency Currency { get; set; }

		public Guid? CreditorId { get; set; }
		public Guid? DebtorId { get; set; }

		public virtual AppUser CreditorUser { get; set; }
		public virtual AppUser DebtorUser { get; set; }

        public DebtorRequestStatus DebtorStatus { get; set; }
		public CreditorRequestStatus CreditorStatus { get; set; }

		public ICollection<Spend> GetSpendCollection(bool forCreditor)
		{
			return forCreditor ? CreditorSpends : DebtorSpends;
		}

		public void SetSpendCollection(bool forCreditor, ICollection<Spend> spends)
		{
			if (forCreditor)
			{
				CreditorSpends = spends;
			}
			else
			{
				DebtorSpends = spends;
			}
		}

		public class DebtorUserDetails
		{
			public Guid UserId { get; set; }
            public DebtorRequestStatus Status { get; set; }
            public AppUser User { get; set; }
        }

		public class CreditorUserDetails
		{
			public Guid UserId { get; set; }
			public CreditorRequestStatus Status { get; set; }
			public AppUser User { get; set; }
		}
	}
}
