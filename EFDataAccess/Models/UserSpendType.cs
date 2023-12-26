using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class UserSpendType
    {
        public Guid UserId { get; set; }
        public int SpendTypeId { get; set; }

        public virtual SpendType SpendType { get; set; }
        public virtual AppUser User { get; set; }
    }
}
