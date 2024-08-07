﻿using System;

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

    public record AccountPeriodIdReqResp(int AccountPeriodIdReq, int? AccountPeriodResp) { }
}