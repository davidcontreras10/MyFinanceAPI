using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class TransferRecord
    {
        public int TransferRecordId { get; set; }
        public int SpendId { get; set; }

        public virtual Spend Spend { get; set; }
    }
}
