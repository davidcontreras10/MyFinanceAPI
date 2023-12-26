using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class UserBankSummaryAccount
    {
        public int AccountId { get; set; }
        public Guid UserId { get; set; }

        public virtual Account Account { get; set; }
        public virtual AppUser User { get; set; }
    }
}
