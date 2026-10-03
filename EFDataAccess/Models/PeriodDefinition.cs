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

        /// <summary>
        /// The period definition the add account form preselects. Meant to be set on one row; it's only a
        /// suggestion, the user can pick another.
        /// </summary>
        public bool IsDefault { get; set; }

        public virtual PeriodType PeriodType { get; set; }
        public virtual ICollection<Account> Account { get; set; }
    }
}
