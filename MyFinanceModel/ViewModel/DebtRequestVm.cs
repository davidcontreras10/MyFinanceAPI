using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ViewModel
{
	public class DebtRequestVm
	{
		public int Id { get; set; }
		public string EventName { get; set; }
		public string EventDescription { get; set; }
		public DateTime EventDate { get; set; }
		public DateTime CreatedDate { get; set; }
		public decimal Amount { get; set; }
		public BasicCurrencyViewModel Currency { get; set; }
		public Creditor Creditor { get; set; }
		public Debtor Debtor { get; set; }
	}

	public class UserDebtRequestVm(Guid userId) : DebtRequestVm
	{
		public bool CreatedByMe => Creditor?.UserId == userId;
	}
}
