using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFunctions.Models
{
	public enum ExecutedTaskStatus
	{
		Unknown = 0,
		Created = 1,
		Succeeded = 2,
		Failed = 3
	}

	public class TaskExecutedResult
	{
		public string TaskId { get; set; }
		public ExecutedTaskStatus Status { get; set; }
		public string ErrorMsg { get; set; }
	}
}
