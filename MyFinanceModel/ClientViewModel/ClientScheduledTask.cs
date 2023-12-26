using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientScheduledTask
	{
		[Required]
		public float Amount { get; set; }
		
		[Range(1,int.MaxValue)]
		[Required]
		public int SpendTypeId { get; set; }

		[Range(1, int.MaxValue)]
		[Required]
		public int CurrencyId { get; set; }
		
		[Required]
		public string Description { get; set; }

		[Range(1, int.MaxValue)]
		[Required]
		public int AccountId { get; set; }

		[Range(1, 2)]
		[Required]
		public int FrequencyType { get; set; }
		
		[Required]
		public IEnumerable<int> Days { get; set; }

		public class Basic : ClientScheduledTask
		{
			public bool IsSpendTrx { get; set; }
		}

		public class Transfer : ClientScheduledTask
		{
			[Range(1, int.MaxValue)]
			[Required]
			public int ToAccountId { get; set; }
		}
	}
}
