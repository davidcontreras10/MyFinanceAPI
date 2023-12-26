using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class LoanRecordStatus
    {
        public LoanRecordStatus()
        {
            LoanRecord = new HashSet<LoanRecord>();
        }

        public int LoanRecordStatusId { get; set; }
        public string LoanRecordStatusName { get; set; }

        public virtual ICollection<LoanRecord> LoanRecord { get; set; }
    }
}
