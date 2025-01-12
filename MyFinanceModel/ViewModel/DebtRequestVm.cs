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
		public decimal Amount { get; set; }
		public BasicCurrencyViewModel Currency { get; set; }
		public AppUser Creditor { get; set; }
		public AppUser Debtor { get; set; }
	}
}
