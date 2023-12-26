using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyFinanceModel.ViewModel;

namespace MyFinanceModel.ClientViewModel
{
	public class ClientExecuteTask
	{
		public ExecuteTaskRequestType RequestType { get; set; }
		public DateTime? DateTime { get; set; }
	}
}
