using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class UserProcessingResponse
	{
        public IReadOnlyCollection<BankTrxItemReqResp> BankTransactions { get; set; }
        public IReadOnlyCollection<SpendItemModified> ItemModifieds { get; set; }
    }
}
