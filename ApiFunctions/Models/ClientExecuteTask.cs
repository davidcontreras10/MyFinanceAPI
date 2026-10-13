using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiFunctions.Models
{
	public enum ExecuteTaskRequestType
	{
		Unknown = 0,
		Manual = 1,
		Automatic = 2
	}

	public class ClientExecuteTask
	{
		public ExecuteTaskRequestType RequestType { get; set; }
		public DateTime? DateTime { get; set; }
	}
}
