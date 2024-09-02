using System;
using System.Collections.Generic;
using System.Diagnostics;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Utilities;

namespace MyFinanceModel.ViewModel
{
	public interface IDropDownSelectable
	{
		int Id { get; }
		string Name { get; }
		bool IsSelected { get; }
	}

	public class BasicDropDownSelectable : IDropDownSelectable
	{
		public int Id { get; set; }

		public string Name { get; set; }

        public bool IsSelected { get; set; }
	}


	public class SupportedAccountIncludeViewModel
    {
        public int AccountId { get; set; }
        public IEnumerable<AccountIncludeViewModel> AccountIncludeViewModels { get; set; }
    }

    public class AccountViewModel : AccountBasicInfo, IDropDownSelectable
    {
        public int GlobalOrder { get; set; }
        public int Id => AccountId;
        public string Name => AccountName;
        public bool IsSelected => false;
    }

    public class AccountDataViewModel : AccountViewModel
    {
        #region Attributes

        public int AccountPeriodId { get; set; }
        public int CurrencyId { get; set; }
        public DateTime InitialDate { get; set; }
        public DateTime EndDate { get; set; }

        #endregion
    }

    public class AccountFinanceViewModel : AccountDataViewModel
    {
        #region Attributes

        public string CurrencySymbol { get; set; }
        public float Budget { get; set; }
        public float Spent { get; set; }
        public float PeriodBalance { get; set; }
        public float GeneralBalance { get; set; }
        public float GeneralBalanceToday { get; set; }
        public IReadOnlyCollection<FinanceSpendViewModel> SpendViewModels { get; set; }
        public string AccountPeriodName => GetAccountPeriodName();
		public TrxFiltersContainer TrxFilters { get; set; }

		#endregion

		#region Num Property

		public string NumBudget => GetAmountValue(Budget);

        public string NumSpent => GetAmountValue(Spent);

        public string NumPeriodBalance => GetAmountValue(PeriodBalance);

        public string NumGeneralBalance => GetAmountValue(GeneralBalance);

        public string NumGeneralBalanceToday => GetAmountValue(GeneralBalanceToday);

        #endregion

        #region Private Methods

        private string GetAmountValue(float value)
        {
            var result = NumUtils.GetCurrencyFormatted(value);
            result = CurrencySymbol + result;
            return result;
        }

        private string GetAccountPeriodName()
        {
            return InitialDate.ToShortDateString() + "-" + EndDate.ToShortDateString();
        }

        #endregion
    }

    public class TransferAccountDataViewModel : AccountFinanceViewModel
    {
        public DateTime UserEndDate => GetUserEndDate();
	    public DateTime SuggestedDate => GetSuggestedDate();
        public IEnumerable<CurrencyViewModel> SupportedCurrencies { get; set; } 
        public IEnumerable<AccountViewModel>  SupportedAccounts { get; set; }
        public IEnumerable<SpendTypeViewModel> SpendTypeViewModels { get; set; }

        #region Methods

	    private DateTime GetSuggestedDate()
	    {
		    var now = DateTime.Now;
		    return now >= InitialDate && now < EndDate ? now : GetUserEndDate();
	    }

        private DateTime GetUserEndDate()
        {
            try
            {
                var dateTime = EndDate.Date;
                dateTime = dateTime.AddSeconds(-1);
                return dateTime;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return DateTime.Now.AddDays(1);
            }
        }

        #endregion
    }

    public class SpendViewModel
    {
        #region Attributes

        public int AccountId { get; set; }
        public int SpendId { get; set; }
        public DateTime SpendDate { get; set; }
        public DateTime? SetPaymentDate { get; set; }
		public DateTime? UtcRecordDate { get; set; }
		public int SpendTypeId { get; set; }
        public string SpendTypeName { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbol { get; set; }
        public float Numerator { get; set; }
        public float Denominator { get; set; }
        public float OriginalAmount { get; set; }
        public string Description { get; set; }
        public int AmountCurrencyId { get; set; }
        public int AmountTypeId { get; set; }
        public bool IsPending { get; set; }
        public float ConvertedAmount => GetConvertedAmount();

        #endregion

        #region Methods

        public bool IsOriginalAmount()
        {
            return (int)Numerator == 1 && (int)Denominator == 1;
        }

        private float GetConvertedAmount()
        {
            return (int)Denominator == 0 ? 0 : (OriginalAmount * Numerator) / Denominator;
        }
        
        #endregion
    }

    public class FinanceSpendViewModel : SpendViewModel
	{
		public bool? HasBankTrx { get; set; }
	}


	public class AddSpendViewModel : AccountDataViewModel
    {
        #region Properties

        public bool IsDefaultPending { get; set; }
        public DateTime UserEndDate => GetUserEndDate();
	    public DateTime SuggestedDate { get; set; } = DateTime.Now;
        public IEnumerable<CurrencyViewModel> SupportedCurrencies { get; set; }
        public IEnumerable<AccountIncludeViewModel> SupportedAccountInclude { get; set; }
        public IEnumerable<SpendTypeViewModel> SpendTypeViewModels { get; set; }

        #endregion

        #region Methods

        public DateTime GetUserEndDate()
        {
            var dateTime = EndDate.Date;
            dateTime = dateTime.AddSeconds(-1);
            return dateTime;
        }

        #endregion
    }

    public class EditSpendViewModel : AddSpendViewModel
    {
        public DateRange PossibleDateRange { get; set; }
        public FinanceSpendViewModel SpendInfo { get; set; }
        public BasicTransferInfo TransferInfo { get; set; }
    }

    public class BasicTransferInfo
    {
        public string SourceAccountName { get; set; }
        public string DestinationAccountName { get; set; }
    }

    public class SpendTypeViewModel : IDropDownSelectable
    {
        public int SpendTypeId { get; set; }
        public string SpendTypeName { get; set; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }

		public int Id => SpendTypeId;
        public string Name => SpendTypeName;
        public bool IsSelected => IsDefault;
    }

    public class CurrencyViewModel : IDropDownSelectable
    {
        #region Attributes

        public int AccountId { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
        public string Symbol { get; set; }
        public IEnumerable<MethodId> MethodIds { get; set; }
        public bool Isdefault { get; set; }
        public string IsoCode { get; set; }

        public int Id => CurrencyId;
        public string Name => CurrencyName;
        public bool IsSelected => Isdefault;

        #endregion
	}

    public class AccountIncludeViewModel : IDropDownSelectable
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public IEnumerable<MethodId> MethodIds { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSelected { get; set; }
        public bool IsCurrentSelection { get; set; }
        public SpendAmount Amount { get; set; }

		public int Id => AccountId;
        public string Name => AccountName;
    }

    public class SpendAmount
    {
        public float Value { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbol { get; set; }
    }

    public class MethodId
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }
        public bool IsSelected { get; set; }
    }
}
