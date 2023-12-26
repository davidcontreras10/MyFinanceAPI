using System;
using System.Collections.Generic;
using MyFinanceModel.Utilities;
using Newtonsoft.Json;

namespace MyFinanceModel.ViewModel
{
	public enum ScheduledTaskType
	{
		Invalid = 0,
		Basic = 1,
		Transfer = 2
	}

	public enum ScheduledTaskFrequencyType
	{
		Invalid = 0,
		Monthly = 1,
		Weekly = 2
	}

	[JsonConverter(typeof(ScheduledTaskVmSerializer))]
	public abstract class BaseScheduledTaskVm
	{
		public Guid Id { get; set; }

		public float Amount { get; set; }

		public int SpendTypeId { get; set; }

		public int CurrencyId { get; set; }

		public string CurrencySymbol { get; set; }

		public string Description { get; set; }

		public int AccountId { get; set; }

		public string AccountName { get; set; }

		public ScheduledTaskFrequencyType FrequencyType { get; set; }

		public IEnumerable<int> Days { get; set; }

		public ExecutedTaskStatus LastExecutedStatus { get; set; }

		public string LastExecutedMsg { get; set; }

		public abstract ScheduledTaskType TaskType { get; }
	}

	public class TransferScheduledTaskVm : BaseScheduledTaskVm
	{
		public int ToAccountId { get; set; }
		public string ToAccountName { get; set; }
		public override ScheduledTaskType TaskType => ScheduledTaskType.Transfer;
	}

	public class BasicScheduledTaskVm : BaseScheduledTaskVm
	{
		public bool IsSpend { get; set; }
		public override ScheduledTaskType TaskType => ScheduledTaskType.Basic;
	}
}
