using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class LoanRecord
    {
        public int LoanRecordId { get; set; }
        public string LoanRecordName { get; set; }
        public int SpendId { get; set; }
        public int LoanRecordStatusId { get; set; }

        public virtual LoanRecordStatus LoanRecordStatus { get; set; }
        public virtual Spend Spend { get; set; }
    }
}
