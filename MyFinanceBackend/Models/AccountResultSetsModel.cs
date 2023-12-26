using System.Collections.Generic;

namespace MyFinanceBackend.Models
{
    internal class AccountAddViewModelResultSet
    {
        public const int TABLE_COUNT = 9;
        public IEnumerable<int> SpendTypeAccountDataResultSetList { get; set; }
        public IEnumerable<AccountInfoResultSet> AccountInfoResultSetList { get; set; }
        public IEnumerable<SpendTypeInfoResultSet> SpendTypeInfoResultSetList { get; set; }
        public IEnumerable<AccountTypeInfoResultSet> AccountTypeInfoResultSetList { get; set; }
        public IEnumerable<PeriodTypeInfoResultSet> PeriodTypeInfoResultSet { get; set; }
        public IEnumerable<FinancialEntityInfoResultSet> FinancialEntityInfoResultSetList { get; set; }
        public IEnumerable<CurrencyDataResultSet> CurrencyDataResultSetList { get; set; }
        public IEnumerable<AccountIncludeAccountInfoResultSet> AccountIncludeAccountInfoResultSetList { get; set; }
        public IEnumerable<AccountGroupResultSet> AccountGroupResultSetList { get; set; } 
    }
    
    internal class AccountEditViewModelResultSet
    {
        public const int TABLE_COUNT = 10;
        public IEnumerable<AccountDetailResultSet> AccountDetailResultSetList { get; set; }
        public IEnumerable<SpendTypeAccountDataResultSet> SpendTypeAccountDataResultSetList { get; set; }
        public IEnumerable<AccountInfoResultSet> AccountInfoResultSetList { get; set; }
        public IEnumerable<SpendTypeInfoResultSet> SpendTypeInfoResultSetList { get; set; }
        public IEnumerable<AccountTypeInfoResultSet> AccountTypeInfoResultSetList { get; set; }
        public IEnumerable<PeriodTypeInfoResultSet> PeriodTypeInfoResultSet { get; set; } 
        public IEnumerable<FinancialEntityInfoResultSet> FinancialEntityInfoResultSetList { get; set; } 
        public IEnumerable<CurrencyDataResultSet> CurrencyDataResultSetList { get; set; }
        public IEnumerable<AccountIncludeAccountInfoResultSet> AccountIncludeAccountInfoResultSetList { get; set; }
        public IEnumerable<AccountGroupResultSet> AccountGroupResultSetList { get; set; } 
    }

    internal class AccountDetailResultSet
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int AccountPosition { get; set; }
        public string AccountHeaderColor { get; set; }
        public int PeriodDefinitionId { get; set; }
        public int CurrencyId { get; set; }
        public int FinancialEntityId { get; set; }
        public int? SpendTypeId { get; set; }
        public int AccountTypeId { get; set; }
	    public float BaseBudget { get; set; }
        public int AccountGroupId { get; set; }
    }

    internal class SpendTypeAccountDataResultSet
    {
        public int AccountId { get; set; }
        public int SpendTypeId { get; set; }
    }

    internal class AccountInfoResultSet
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
    }

    internal class AccountIncludeInfoResultSet : AccountInfoResultSet
    {
        public int CurrencyConverterMethodId { get; set; }
        public string CurrencyConverterMethodName { get; set; }
        public int FinancialEntityId { get; set; }
        public string FinancialEntityName { get; set; }
        public bool IsSelected { get; set; }
        public bool IsDefault { get; set; }
    }

    internal class AccountIncludeAccountInfoResultSet : AccountIncludeInfoResultSet
    {
        public int AccountIncludeId { get; set; }
        public string AccountIncludeName { get; set; }
    }

    internal class AccountTypeInfoResultSet
    {
        public int AccountTypeId { get; set; }
        public string AccountTypeName { get; set; }
    }

    internal class PeriodTypeInfoResultSet
    {
        public int PeriodDefinitionId { get; set; }
        public int PeriodTypeId { get; set; }
        public string PeriodTypeName { get; set; }
        public string CuttingDate { get; set; }
        public int Repetition { get; set; }
    }

    internal class FinancialEntityInfoResultSet
    {
        public int FinancialEntityId { get; set; }
        public string FinancialEntityName { get; set; }
    }
}
