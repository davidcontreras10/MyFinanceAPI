using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AccountPeriod
    {
        public AccountPeriod()
        {
            SpendOnPeriod = new HashSet<SpendOnPeriod>();
        }

        public int AccountPeriodId { get; set; }
        public int? AccountId { get; set; }
        public float? Budget { get; set; }
        public DateTime? InitialDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CurrencyId { get; set; }

        public virtual Account Account { get; set; }
        public virtual Currency Currency { get; set; }
        public virtual ICollection<SpendOnPeriod> SpendOnPeriod { get; set; }
    }
}
