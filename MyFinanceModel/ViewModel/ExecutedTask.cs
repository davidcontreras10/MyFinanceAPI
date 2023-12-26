using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ViewModel
{
	public enum ExecuteTaskRequestType
	{
		Unknown = 0,
		Manual = 1,
		Automatic = 2
	}

	public enum ExecutedTaskStatus
	{
		Unknown = 0,
		Created = 1,
		Succeeded = 2,
		Failed = 3
	}

	public class ClientExecutedTask
	{
		public string AutomaticTaskId { get; set; }
		public string ExecutedByUserId { get; set; }
		public DateTime ExecuteDatetime { get; set; }
		public ExecutedTaskStatus ExecutionStatus { get; set; }
		public string ExecutionMsg { get; set; }
	}
}
