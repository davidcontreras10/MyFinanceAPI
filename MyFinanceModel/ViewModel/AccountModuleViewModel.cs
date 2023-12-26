using System.Collections.Generic;

namespace MyFinanceModel.ViewModel
{
    public class AccountMainViewModel
    {
        public IEnumerable<AccountDetailsViewModel> AccountDetailsViewModels { get; set; }
        public IEnumerable<AccountGroupViewModel> AccountGroupViewModels { get; set; }
    }

    public class AccountDetailsViewModel : AccountViewModel
    {
        public int AccountPosition { get; set; }
        public int AccountGroupId { get; set; }
        public FrontStyleData AccountStyle { get; set; }
	    public float BaseBudget { get; set; }
    }

    public class AccountDetailsPeriodViewModel : AccountDetailsViewModel
    {
	    public int AccountPeriodId { get; set; }
    }


	public class AddAccountViewModel
    {
        public FrontStyleData AccountStyle { get; set; }
        public float BaseBudget { get; set; }
        public string AccountName { get; set; }
        public IEnumerable<SpendTypeViewModel> SpendTypeViewModels { get; set; }
        public IEnumerable<AccountTypeViewModel> AccountTypeViewModels { set; get; }
        public IEnumerable<PeriodTypeViewModel> PeriodTypeViewModels { set; get; }
        public IEnumerable<FinancialEntityViewModel> FinancialEntityViewModels { get; set; }
        public IEnumerable<AccountIncludeViewModel> AccountIncludeViewModels { get; set; }
        public IEnumerable<CurrencyViewModel> CurrencyViewModels { get; set; }
        public IEnumerable<AccountGroupViewModel> AccountGroupViewModels { get; set; } 
    }

    public class AccountDetailsInfoViewModel : AccountDetailsViewModel
	{
		public IEnumerable<SpendTypeViewModel> SpendTypeViewModels { get; set; }
		public IEnumerable<AccountTypeViewModel> AccountTypeViewModels { set; get; } 
		public IEnumerable<PeriodTypeViewModel> PeriodTypeViewModels { set; get; }
	    public IEnumerable<FinancialEntityViewModel> FinancialEntityViewModels { get; set; }
        public IEnumerable<AccountIncludeViewModel> AccountIncludeViewModels { get; set; } 
		public IEnumerable<CurrencyViewModel> CurrencyViewModels { get; set; }
        public IEnumerable<AccountGroupViewModel> AccountGroupViewModels { get; set; } 
	}

	public class AccountTypeViewModel : IDropDownSelectable
	{
		public int AccountTypeId { get; set; }
		public string AccountTypeName { get; set; }
		public bool IsDefault { get; set; }

		public int Id => AccountTypeId;
	    public string Name => AccountTypeName;
	    public bool IsSelected => IsDefault;
	}

	public class PeriodTypeViewModel
	{
		public int PeriodDefinitionId { get; set; }
		public int PeriodTypeId { get; set; }
		public string PeriodTypeName { get; set; }
		public string CuttingDate { get; set; }
		public int Repetition { get; set; }
		public bool IsDefault { get; set; }

		public int Id => PeriodDefinitionId;
	    public string Name => GetName();
	    public bool IsSelected => IsDefault;

	    private string GetName()
		{
			return $"{PeriodTypeName} CD: {CuttingDate} RPT: {Repetition}";
		}
	}

	public class FinancialEntityViewModel
	{
		public int FinancialEntityId { get; set; }
		public string FinancialEntityName { get; set; }
		public bool IsDefault { get; set; }

		public int Id => FinancialEntityId;
	    public string Name => FinancialEntityName;
	    public bool IsSelected => IsDefault;
	}
}