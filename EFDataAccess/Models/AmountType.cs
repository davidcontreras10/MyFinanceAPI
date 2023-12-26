using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AmountType
    {
        public AmountType()
        {
            Spend = new HashSet<Spend>();
        }

        public int AmountTypeId { get; set; }
        public string AmountTypeName { get; set; }
        public int AmountSign { get; set; }

        public virtual ICollection<Spend> Spend { get; set; }
    }
}
