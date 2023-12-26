using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class Currency
    {
        public Currency()
        {
            Account = new HashSet<Account>();
            AccountPeriod = new HashSet<AccountPeriod>();
            AutomaticTask = new HashSet<AutomaticTask>();
            Spend = new HashSet<Spend>();
        }

        public int CurrencyId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }

        public virtual ICollection<Account> Account { get; set; }
        public virtual ICollection<AccountPeriod> AccountPeriod { get; set; }
        public virtual ICollection<AutomaticTask> AutomaticTask { get; set; }
        public virtual ICollection<Spend> Spend { get; set; }
    }
}
