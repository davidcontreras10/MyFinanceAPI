using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel
{
    public class AccountCurrencyConverterData
    {
        #region Attributes

        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int AmountCurrencyId { get; set; }
        public int AccountCurrencyId { get; set; }

        #endregion 
    }
}
