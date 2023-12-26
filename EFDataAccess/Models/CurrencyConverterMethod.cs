using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class CurrencyConverterMethod
    {
        public CurrencyConverterMethod()
        {
            AccountInclude = new HashSet<AccountInclude>();
            SpendOnPeriod = new HashSet<SpendOnPeriod>();
        }

        public int CurrencyConverterMethodId { get; set; }
        public int CurrencyConverterId { get; set; }
        public string Name { get; set; }
        public bool? IsDefault { get; set; }
        public int? FinancialEntityId { get; set; }

        public virtual CurrencyConverter CurrencyConverter { get; set; }
        public virtual ICollection<AccountInclude> AccountInclude { get; set; }
        public virtual ICollection<SpendOnPeriod> SpendOnPeriod { get; set; }
        public virtual FinancialEntity FinancialEntity { get; set; }
    }
}
