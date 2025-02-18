﻿using System;
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
			DebtorDebtRequests = new HashSet<EFDebtRequest>();
			CreditorDebtRequests = new HashSet<EFDebtRequest>();
            UserRoles = new HashSet<EFAppRole>();
		}

        public string Username { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public Guid UserId { get; set; }
        public string PrimaryEmail { get; set; }

        public virtual ICollection<EFAppRole> UserRoles { get; set; }
        public virtual ICollection<Account> Account { get; set; }
        public virtual ICollection<AccountGroup> AccountGroup { get; set; }
        public virtual ICollection<AutomaticTask> AutomaticTask { get; set; }
        public virtual ICollection<ExecutedTask> ExecutedTask { get; set; }
        public virtual ICollection<AppUser> InverseCreatedByUser { get; set; }
        public virtual ICollection<UserBankSummaryAccount> UserBankSummaryAccount { get; set; }
        public virtual ICollection<UserSpendType> UserSpendType { get; set; }
		public virtual ICollection<EFDebtRequest> DebtorDebtRequests { get; set; }
		public virtual ICollection<EFDebtRequest> CreditorDebtRequests { get; set; }
	}
}
