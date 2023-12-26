using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
    public class AddLoanRecordViewModel
    {
        #region Attributes

        public IEnumerable<CurrencyViewModel> PossibleCurrencyViewModels { get; set; }

        public AccountPeriodBasicInfo AccountInfo { get; set; }
        
        #endregion
    }
}
