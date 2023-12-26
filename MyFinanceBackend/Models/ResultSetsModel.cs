using System;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Models
{
    internal class AccountFinanceResultSet
    {
        #region Attributes

        public int AccountId { set; get; }
        public string AccountName { set; get; }
        public int AccountPeriodId { set; get; }
        public int AccountCurrencyId { set; get; }
        public string AccountCurrencySymbol { set; get; }
        public DateTime InitialDate { set; get; }
        public DateTime EndDate { set; get; }
        public float Budget { set; get; }
        public int SpendId { set; get; }
        public float SpendAmount { set; get; }
        public float Numerator { set; get; }
        public float Denominator { set; get; }
        public DateTime SpendDate { set; get; }
        public string SpendTypeName { set; get; }
        public string SpendCurrencyName { set; get; }
        public string SpendCurrencySymbol { set; get; }
        public float GeneralBalance { get; set; }
        public float GeneralBalanceToday { get; set; }
        public float PeriodBalance { get; set; }
        public float AccountPeriodSpent { get; set; }
        public int AmountType { get; set; }
        public DateTime? SetPaymentDate { get; set; }
        public bool IsPending { get; set; }
        public string SpendDescription { get; set; }
        
        #endregion
    }

    #region Edit Spend

    internal class DateRangeResultSet
    {
        #region Attributes

        #region Properties

        public int AccountId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public bool IsValid { get; set; }
        public bool? IsDateValid { get; set; }

        #endregion

        #endregion
    }

    internal class SpendIncludeModelResultSet
    {
        public int AccountId { get; set; }
        public int AccountPeriodId { get; set; }
        public int AccountIncludeId { get; set; }
        public float ConvertedAmount { get; set; }
        public bool IsSelected { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbol { get; set; }
    }

	internal class SpendModelResultSet
	{
		public int AccountId { get; set; }
		public int SpendId { set; get; }
		public int AmountCurrencyId { get; set; }
		public DateTime SpendDate { get; set; }
	    public DateTime? SetPaymentDate { get; set; }
        public int SpendTypeId { get; set; }
		public float OriginalAmount { get; set; }
		public int Numerator { get; set; }
		public int Denominator { get; set; }
		public string SpendDescription { get; set; }
		public int CurrencyConverterMethodId { get; set; }
        public bool IsPending { get; set; }
        public int AmountTypeId { get; set; }
    }

	#endregion

    #region Add Spend

    internal class SavedSpendResultSet
    {
        public string UserId { get; set; }
        public int SpendId { get; set; }
        public TransactionTypeIds AmountTypeId { get; set; }
        public float Amount { get; set; }
        public float? AmountNumerator { get; set; }
        public float? AmountDenominator { get; set; }
        public DateTime SpendDate { get; set; }
        public int CurrencyId { get; set; }
        public int AccountId { get; set; }
        public int CurrencyConvertionMethodId { get; set; }
        public bool IsOriginal { get; set; }
        public bool IsPending { get; set; }
    }

    internal class AccountIncludeMethodResultSet
    {
        public int AccountId { get; set; }
        public int AccountIncludeId { get; set; }
        public int CurrencyConverterMethodId { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCurrentSelection { get; set; }
    }

    internal class AccountCurrencyResultSet
    {
        public int AccountId { get; set; }
        public int CurrencyId { get; set; }
        public int CurrencyConverterMethodId { get; set; }
        public bool IsSuggested { get; set; }
    }

    internal class SpendTypeDefaultResultSet
    {
        public int AccountId { get; set; }
        public int SpendTypeIdDefault { get; set; }
    }

    /* Data */
    internal class CurrencyConverterMethodDataResultSet
    {
        public int CurrencyConverterMethodId { get; set; }
        public string CurrencyConverterMethodName { get; set; }

        public bool IsDefault { get; set; }
    }

    internal class SupportedAccountDataResultSet
    {
        public int AccountId { get; set; }
        public string Name { get; set; }
    }

    internal class CurrencyDataResultSet
    {
        public int CurrencyId { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
    }

    internal class SpendTypeInfoResultSet
    {
        public int SpendTypeId { get; set; }
        public string SpendTypeName { get; set; }
        public string SpendTypeDescription { get; set; }
    }

    internal class AccountIncludeDataResultSet
    {
        public int AccountId { get; set; }
        public int AccountIncludeId { get; set; }
        public int CurrencyConverterMethodId { get; set; }
    }

    internal class AccountDataResultSet
    {
        public int AccountId { set; get; }
        public int AccountPeriodId { set; get; }
        public int AccountCurrencyId { set; get; }
        public DateTime InitialDate { set; get; }
        public DateTime EndDate { set; get; }
        public string AccountName { get; set; }
    }

    #endregion

    #region add spend validation

    public class ClientAddSpendValidationResultSet
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int AmountCurrencyId { get; set; }
        public int ConvertCurrencyId { get; set; }
        public int CurrencyIdOne { get; set; }
        public int CurrencyIdTwo { get; set; }
        public bool IsSuccess { get; set; }
    }

    #endregion
    
    #region Transfers

    internal class AccountMethodConvertionInfo
    {
        public int AccountId { get; set; }
        public int CurrencyConverterMethodId { get; set; }
    }

    #endregion

    #region Accounts

    internal class AccountCurrencyInfo
    {
        public int AccountId { get; set; }
        public int CurrencyId { get; set; }
    }

    #endregion

    #region Loan

    public class DetailLoanResultSet
    {
        public int LoanRecordId { get; set; }
        public string LoanRecordName { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public float PaymentSummary { get; set; }

        public SpendViewModel LoanSpendRecord { get; set; }
        public SpendViewModel Spend { get; set; }
    }

    #endregion
}
