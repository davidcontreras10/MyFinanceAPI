using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class PeriodType
    {
        public PeriodType()
        {
            PeriodDefinition = new HashSet<PeriodDefinition>();
        }

        public int PeriodTypeId { get; set; }
        public string Name { get; set; }

        public virtual ICollection<PeriodDefinition> PeriodDefinition { get; set; }
    }
}
