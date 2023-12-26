using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
    public partial class AppUser
    {
        public AppUser()
        {
            Account = new HashSet<Account>();
            AccountGroup = new HashSet<AccountGroup>();
            AutomaticTask = new HashSet<AutomaticTask>();
            ExecutedTask = new HashSet<ExecutedTask>();
            InverseCreatedByUser = new HashSet<AppUser>();
            UserBankSummaryAccount = new HashSet<UserBankSummaryAccount>();
            UserSpendType = new HashSet<UserSpendType>();
        }

        public string Username { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public Guid UserId { get; set; }
        public string PrimaryEmail { get; set; }
        public Guid? CreatedByUserId { get; set; }

        public virtual AppUser CreatedByUser { get; set; }
        public virtual ICollection<Account> Account { get; set; }
        public virtual ICollection<AccountGroup> AccountGroup { get; set; }
        public virtual ICollection<AutomaticTask> AutomaticTask { get; set; }
        public virtual ICollection<ExecutedTask> ExecutedTask { get; set; }
        public virtual ICollection<AppUser> InverseCreatedByUser { get; set; }
        public virtual ICollection<UserBankSummaryAccount> UserBankSummaryAccount { get; set; }
        public virtual ICollection<UserSpendType> UserSpendType { get; set; }
    }
}
