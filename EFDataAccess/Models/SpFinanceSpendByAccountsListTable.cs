using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class SpFinanceSpendByAccountsListTable
    {
        public int? AccountId { get; set; }
        public int? AccountPeriodId { get; set; }
        public int? AccountCurrencyId { get; set; }
        public string AccountCurrencySymbol { get; set; }
        public double? AccountGeneralBalance { get; set; }
        public double? AccountGeneralBalanceToday { get; set; }
        public double? AccountPeriodBalance { get; set; }
        public double? AccountPeriodSpent { get; set; }
        public DateTime? InitialDate { get; set; }
        public DateTime? EndDate { get; set; }
        public double? Budget { get; set; }
        public int? SpendId { get; set; }
        public double? SpendAmount { get; set; }
        public double? Numerator { get; set; }
        public double? Denominator { get; set; }
        public DateTime? SpendDate { get; set; }
        public string SpendTypeName { get; set; }
        public string SpendCurrencyName { get; set; }
        public string SpendCurrencySymbol { get; set; }
        public int? AmountType { get; set; }
        public string AccountName { get; set; }
        public bool? IsPending { get; set; }
        public DateTime? SetPaymentDate { get; set; }
        public bool? IsValid { get; set; }
        public bool? IsLoan { get; set; }
        public string SpendDescription { get; set; }
    }
}
