using System;
using System.Collections.Generic;
using System.Diagnostics;
using MyFinanceModel.Utilities;

namespace MyFinanceModel.ViewModel
{
	public interface IDropDownSelectable
	{
		int Id { get; }
		string Name { get; }
		bool IsSelected { get; }
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
        public List<SpendViewModel> SpendViewModels { get; set; }
        public object SpendTable { get; set; }
        public string AccountPeriodName => GetAccountPeriodName();

        #endregion

        #region Num Property

// ReSharper disable InconsistentNaming
        public string numBudget => GetAmountValue(Budget);

        public string numSpent => GetAmountValue(Spent);

        public string numPeriodBalance => GetAmountValue(PeriodBalance);

        public string numGeneralBalance => GetAmountValue(GeneralBalance);

        public string numGeneralBalanceToday => GetAmountValue(GeneralBalanceToday);
        // ReSharper restore InconsistentNaming

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

    public class AddSpendViewModel : AccountDataViewModel
    {
        #region Properties

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
        public SpendViewModel SpendInfo { get; set; }
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
