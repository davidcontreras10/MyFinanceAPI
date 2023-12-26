using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
	public partial class Account
    {
        public Account()
        {
            AccountIncludeAccount = new HashSet<AccountInclude>();
            AccountIncludeAccountIncludeNavigation = new HashSet<AccountInclude>();
            AccountPeriod = new HashSet<AccountPeriod>();
            AutomaticTask = new HashSet<AutomaticTask>();
            TransferTrxDef = new HashSet<TransferTrxDef>();
            UserBankSummaryAccount = new HashSet<UserBankSummaryAccount>();
        }

        public int AccountId { get; set; }
        public int PeriodDefinitionId { get; set; }
        public string Name { get; set; }
        public int? CurrencyId { get; set; }
        public float? BaseBudget { get; set; }
        public int? Position { get; set; }
        public string HeaderColor { get; set; }
        public int AccountTypeId { get; set; }
        public int? DefaultSpendTypeId { get; set; }
        public int? FinancialEntityId { get; set; }
        public Guid? UserId { get; set; }
        public int? AccountGroupId { get; set; }

        public virtual AccountGroup AccountGroup { get; set; }
        public virtual AccountType AccountType { get; set; }
        public virtual Currency Currency { get; set; }
        public virtual SpendType DefaultSpendType { get; set; }
        public virtual FinancialEntity FinancialEntity { get; set; }
        public virtual PeriodDefinition PeriodDefinition { get; set; }
        public virtual AppUser User { get; set; }
        public virtual AccountNotes Notes { get; set; }
        public virtual ICollection<AccountInclude> AccountIncludeAccount { get; set; }
        public virtual ICollection<AccountInclude> AccountIncludeAccountIncludeNavigation { get; set; }
        public virtual ICollection<AccountPeriod> AccountPeriod { get; set; }
        public virtual ICollection<AutomaticTask> AutomaticTask { get; set; }
        public virtual ICollection<TransferTrxDef> TransferTrxDef { get; set; }
        public virtual ICollection<UserBankSummaryAccount> UserBankSummaryAccount { get; set; }
    }

	[Owned]
	public class AccountNotes
	{
		public string Title { get; set; }
		public string Content { get; set; }
	}
}
    