using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
    public class MainViewModel
    {
        public IEnumerable<AccountGroupMainViewViewModel> AccountGroupMainViewViewModels { get; set; } 
    }

    public class AccountGroupMainViewViewModel : AccountGroupViewModel
    {
        public IEnumerable<MainViewModelAccount> MainViewModelAccounts { get; set; }
    }

    public class MainViewModelAccount
    {
        #region Attributes

        public int AccountId { get; set; }

        public string AccountName { get; set; }

        public int Position { get; set; }

        public int CurrentPeriodId { get; set; }

        public int CurrencyId { get; set; }

        public string CurrencyName { get; set; }

        public IList<AccountPeriod> AccountPeriods { get; set; }

        public FrontStyleData FrontStyle { get; set; }

        public AccountType Type
        {
            get => _accountType;
            set => _accountType = value;
        }

        public string GetTableTypeSyle()
        {
            switch (_accountType)
            {
                case AccountType.Checking:
                    return "table-checking";

                case AccountType.Saving:
                    return "table-savings";

                case AccountType.Bank:
                    return "table-bank";

                case AccountType.Undefined:
                    throw new Exception("Invalid account type");

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool SimpleTable => Type == AccountType.Saving;

        public string BorderStyleValue => GetBorderStyleValue();

        public string HeaderStyleValue => GetHeaderStyleValue();

        #endregion

        #region Privates

        private AccountType _accountType;

        #endregion

        public enum AccountType
        {
            Undefined = 0,
            Checking = 1,
            Saving = 2,
            Bank = 3
        }

        #region Methods

        private string GetBorderStyleValue()
        {
            return string.IsNullOrEmpty(FrontStyle.BorderColor) ? "" : $"style='border: solid {FrontStyle.BorderColor} 5px;'";
        }

        private string GetHeaderStyleValue()
        {
            return string.IsNullOrEmpty(FrontStyle.HeaderColor) ? "" : $"style='background: {FrontStyle.HeaderColor};'";
        }

        #endregion
    }

    public class FrontStyleData
    {
	    private string _headerColor;
	    private string _borderColor;

	    public string HeaderColor
	    {
            get => string.IsNullOrEmpty(_headerColor) ? "#ffffff" : _headerColor;
	        set => _headerColor = value;
	    }

	    public string BorderColor
	    {
			get => string.IsNullOrEmpty(_borderColor) ? "#5f9ea0" : _borderColor;
	        set => _borderColor = value;
	    }
    }
}
