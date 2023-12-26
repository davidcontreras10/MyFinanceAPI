using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
    public class LoanReportViewModel
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string LoanName { get; set; }
        public int LoanRecordId { get; set; }
        public IEnumerable<SpendViewModel> SpendViewModels { get; set; }
        public SpendViewModel LoanSpendViewModel { get; set; }
        public float PaymentSumary { get; set; }
        public float PaymentPending => LoanSpendViewModel.ConvertedAmount - PaymentSumary;
    }
}
