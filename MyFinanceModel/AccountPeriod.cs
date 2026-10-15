using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public class AccountPeriod
    {
        #region Attributes
        public bool IsBasicMontly { get; set; }
        public int AccountPeriodId { get; set; }
        public int AccountId { get; set; }
        public float Budget { get; set; }
        public DateTime InitialDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Name => GetDateInfo(true);
        public string UserId { get; set; }

	    #endregion

        #region Methods

        public virtual string GetDateInfo(bool prefix)
        {
            var initial = prefix ? "Period: " : "";
            if(IsBasicMontly)
            {
                initial += $"{InitialDate.ToString("MMMM")} - {InitialDate.Year}";
                return initial;
			}

            return string.Format(initial + "{0} - {1}", InitialDate.ToShortDateString(), EndDate.AddDays(-1).ToShortDateString());
        }

        
        #endregion
    }

    public class AccountPeriodBasicInfo : AccountPeriod
    {
        public string AccountName { get; set; }
    }

    public class AccountBasicInfo
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
    }

    public class AccountBasicPeriodInfo : AccountBasicInfo
    {
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }

	public class AccountPeriodBasicId
	{
		public int AccountPeriodId { get; set; }
		public int AccountId { get; set; }
	}

    public class BankAccountPeriodBasicId : AccountPeriodBasicId
	{
		public int? FinancialEntityId { get; set; }
		public string FinancialEntityName { get; set; }
	}

    public class BankFlaggedAccountBasicInfo : AccountBasicInfo
	{
		public int CurrencyId { get; set; }
		public int? FinancialEntityId { get; set; }
		public string FinancialEntityName { get; set; }
	}

    public class AccountIncludeEdge
	{
		public int AccountId { get; set; }
		public int AccountIncludeId { get; set; }
	}

	/// <summary>Facts about the account tree needed to validate a requested main account.</summary>
	public class AccountHierarchyInfo
	{
		/// <summary>Of the requested main accounts, the ones owned by the user.</summary>
		public HashSet<int> OwnedAccountIds { get; set; } = new HashSet<int>();

		/// <summary>Of the requested main accounts, the ones that are already sub-accounts.</summary>
		public HashSet<int> AccountIdsWithParent { get; set; } = new HashSet<int>();

		/// <summary>How many sub-accounts the account being saved currently has (0 for a new account).</summary>
		public int SubAccountsCount { get; set; }
	}

    public record AccountPeriodIdReqResp(int AccountPeriodIdReq, int? AccountPeriodResp) { }
}