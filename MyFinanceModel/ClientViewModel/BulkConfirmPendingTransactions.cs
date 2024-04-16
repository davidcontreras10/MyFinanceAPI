using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ClientViewModel
{
	public class BulkConfirmPendingTransactions
	{
		[MinLength(1)]
		[Required]
        public IReadOnlyCollection<int> TransactionIds { get; set; }

		[Required]
		public DateTime NewDateTime { get; set; }
    }
}
