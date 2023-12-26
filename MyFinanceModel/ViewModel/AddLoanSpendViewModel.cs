using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
    public class AddLoanSpendViewModel
    {
        public IEnumerable<CurrencyViewModel> PossibleCurrencyViewModels { get; set; }
        public AccountBasicPeriodInfo AccountInfo { get; set; }
        public DateTime SuggestedDate { get; set; } = DateTime.Now;
        public int LoanRecordId { get; set; }
    }
}