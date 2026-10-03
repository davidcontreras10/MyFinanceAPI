using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AccountType
    {
        public AccountType()
        {
            Account = new HashSet<Account>();
        }

        public int AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }

        /// <summary>
        /// Inactive types can't be chosen for new accounts, but accounts that already have one keep it.
        /// </summary>
        public bool IsActive { get; set; }

        public virtual ICollection<Account> Account { get; set; }
    }
}
