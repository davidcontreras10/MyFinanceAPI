using System;
using System.ComponentModel.DataAnnotations;

namespace MyFinanceModel.ClientViewModel
{
    public class ClientLoanViewModel : ClientBasicAddSpend
    {
        [Required]
        public string LoanName { get; set; }

        [Required]
        [Range(1,double.MaxValue)]
        public int AccountId { get; set; }

        [Range(0, double.MaxValue)]
        public int DestinationAccountId { get; set; }
    }
}
