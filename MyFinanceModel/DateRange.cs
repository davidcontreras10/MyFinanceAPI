using System;

namespace MyFinanceModel
{
    public class DateRange
    {
        #region Properties

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public bool IsValid { get; set; }
        public bool? IsDateValid { get; set; } 

        #endregion
    }
}
