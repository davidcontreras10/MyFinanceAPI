using System;

namespace MyFinanceModel
{
    public class AddPeriodData
    {
        public AddPeriodData()
        {
            NextInitialDate = DateTime.Today;
            NextInitialDate = null;
            HasPeriods = false;
            AccountId = 0;
            IsValid = false;    
        }

        #region Attributes

        public int AccountId { get; set; }
        public DateTime? NextInitialDate { get; set; }
        public float Budget { get; set;  }
        public bool HasPeriods { get; set; }
        public bool IsValid { set; get; }

        #endregion
    }
}
