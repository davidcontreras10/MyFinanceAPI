using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class SpendOnPeriod
    {
        public int SpendId { get; set; }
        public int AccountPeriodId { get; set; }
        public double? Numerator { get; set; }
        public double? Denominator { get; set; }
        public bool? PendingUpdate { get; set; }
        public int? CurrencyConverterMethodId { get; set; }
        public bool? IsOriginal { get; set; }

        public virtual AccountPeriod AccountPeriod { get; set; }
        public virtual CurrencyConverterMethod CurrencyConverterMethod { get; set; }
        public virtual Spend Spend { get; set; }

        public double GetAmount()
        {
            var spendAmount = Spend.GetAmount(true);
            if (Denominator == null || Numerator == null || Denominator == 0 || Numerator == 0)
            {
                return spendAmount;
            }

            return (spendAmount * Numerator.Value) / Denominator.Value;
        }
    }
}
