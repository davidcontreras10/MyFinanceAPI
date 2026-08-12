using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyFinanceModel.ViewModel;

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

		[Range(1, 3)]
		[Required]
		public ScheduledTaskFrequencyType FrequencyType { get; set; }

		[Required]
		public IEnumerable<int> Days { get; set; }

		public bool IsPending { get; set; }

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

	public class ClientEditScheduledTask
	{
		public string TaskId { get; set; }

		public float Amount { get; set; }

		public int SpendTypeId { get; set; }

		public bool IsPending { get; set; }

		public string Description { get; set; }

		[Range(1, 3)]
		public ScheduledTaskFrequencyType FrequencyType { get; set; }

		public IEnumerable<int> Days { get; set; }

		[Required]
		public IEnumerable<ScheduledTaskField> ModifyList { get; set; }

		public enum ScheduledTaskField
		{
			Invalid = 0,
			Amount = 1,
			SpendTypeId = 2,
			IsPending = 3,
			Description = 4,
			FrequencyType = 5,
			Days = 6
		}
	}
}
