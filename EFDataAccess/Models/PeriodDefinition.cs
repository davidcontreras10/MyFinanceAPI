using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class PeriodDefinition
    {
        public PeriodDefinition()
        {
            Account = new HashSet<Account>();
        }

        public int PeriodDefinitionId { get; set; }
        public int PeriodTypeId { get; set; }
        public string CuttingDate { get; set; }
        public int? Repetition { get; set; }

        public virtual PeriodType PeriodType { get; set; }
        public virtual ICollection<Account> Account { get; set; }
    }
}
