using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ViewModel
{
	public class ExecutedTaskViewModel
	{
		public ExecutedTaskStatus Status { get; set; }
		public DateTime ExecutedDate { get; set; }
		public string Message { get; set; }
	}
}
