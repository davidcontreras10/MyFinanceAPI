using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class SpendType
    {
        public SpendType()
        {
            Account = new HashSet<Account>();
            AutomaticTask = new HashSet<AutomaticTask>();
            Spend = new HashSet<Spend>();
            UserSpendType = new HashSet<UserSpendType>();
        }

        public int SpendTypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public virtual ICollection<Account> Account { get; set; }
        public virtual ICollection<AutomaticTask> AutomaticTask { get; set; }
        public virtual ICollection<Spend> Spend { get; set; }
        public virtual ICollection<UserSpendType> UserSpendType { get; set; }
    }
}
