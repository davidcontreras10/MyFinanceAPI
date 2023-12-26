using System;
using System.Collections.Generic;

namespace MyFinanceModel.WebMethodsModel
{
    public class BaseMethodModel
    {
        public string UserId { set; get; }
    }

    public class ExchangeRateResultModel
    {
        public IEnumerable<MethodParam> MethodIds { get; set; }
        public DateTime DateTime { get; set; }

        public class MethodParam
        {
            public int Id { get; set; }
            public bool IsPurchase { get; set; }
        }
    }

    public class EditSpendModel : BaseMethodModel
    {
        public int SpendId { set; get; }
        public string ModifyList { set; get; }
        public DateTime SpendDate { set; get; }
        public float Amount { set; get; }
        public string AccountIds { set; get; }
        public string Description { set; get; }
        public int SpendTypeId { set; get; }
    }

    public class GetDateRangeModel : BaseMethodModel
    {
        public string AccountIds { set; get; }
        public DateTime Date { set; get; }
        public bool DateSpecified { set; get; }
    }

    public class AddPeriodModel : BaseMethodModel
    {
        public int AccountId { set; get; }
        public float Budget { set; get; }
        public DateTime Initial { set; get; }
        public DateTime End { set; get; }
    }

    public class GetSpendsInfoModel : BaseMethodModel
    {
        public int? SpendTypeId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndTime { get; set; }
    }


    //string userId, int spendType, DateTime date, double amount, string accountPeriodIds
    public class AddSpendModel : BaseMethodModel
    {
        public int SpendType { get; set; }
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string AccountPeriodIds { get; set; }    
    }

    //string userId, int spendType, DateTime date, double amount, string accountIds
    public class AddSpendByAccountModel : BaseMethodModel
    {
        public int SpendType { get; set; }
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string AccountIds { get; set; }
    }
}
