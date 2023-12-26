using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public class Spend
    {
        #region Attributes

        public string Description { get; set; }
        public int Id { set; get; }
        public int AccountId { get; set; }
        public int AccountPeriodId { get; set; }
        public double Amount { set; get; }
        public SpendType Type { set; get; }
        public int SpendTypeId { set; get; }
        public DateTime Date { set; get; }
        public string UserId { set; get; }
        public List<SpendType> SpendTypesTest { set; get; }
        public int[] AccountIdsArray { set; get; }
        public string AccountIds { set; get; }
        public Fields SpendFields => new Fields();
        public bool IsPending { get; set; }

        #endregion

        public class Fields
        {
            public int AccountIds => 3;

            public int Amount => 2;

            public int SpendTypeId => 5;

            public int Description => 4;

            public int Date => 1;
        }
    }
}
