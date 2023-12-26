using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class SpendDependencies
    {
        public int SpendId { get; set; }
        public int DependencySpendId { get; set; }

        public virtual Spend Spend { get; set; }

        public virtual Spend DependencySpend { get; set; }
	}
}
