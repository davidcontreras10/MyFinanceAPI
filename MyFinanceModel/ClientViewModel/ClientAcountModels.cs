using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MyFinanceModel.ViewModel;

namespace MyFinanceModel.ClientViewModel
{
    public enum AccountFiedlds
    {
        AccountName = 1,
        //PeriodDefinitionId = 2,
        //CurrencyId = 3,
        BaseBudget = 4,
        HeaderColor = 5,
        AccountTypeId = 6,
        SpendTypeId = 7,
        FinancialEntityId = 8,
        AccountIncludes = 9,
        AccountGroupId = 10
    }

    public class ClientAddAccount
    {
		[MinLength(2)]
		[MaxLength(100)]
		[Required]
        public string AccountName { get; set; }

		[Required]
        public int PeriodDefinitionId { get; set; }

		[Required]
		public int CurrencyId { get; set; }
        
		[Range(0,float.MaxValue)]
		public float? BaseBudget { get; set; }

		public FrontStyleData HeaderColor { get; set; }
        
		public FullAccountInfoViewModel.AccountType AccountTypeId { get; set; }
        
		public int? SpendTypeId { get; set; }
        
		public int? FinancialEntityId { get; set; }
        
		[Required]
		public int AccountGroupId { get; set; }
        
		public IEnumerable<ClientAccountInclude> AccountIncludes { get; set; }
    }

	public class ClientEditAccount
	{
		public int AccountId { get; set; }
		public string AccountName { get; set; }
		//public int? PeriodDefinitionId { get; set; }
		//public int? CurrencyId { get; set; }
		public float? BaseBudget { get; set; }
		public FrontStyleData HeaderColor { get; set; }
		public FullAccountInfoViewModel.AccountType AccountTypeId { get; set; }
		public int? SpendTypeId { get; set; }
		public int? FinancialEntityId { get; set; }
        public int AccountGroupId { get; set; }
		public IEnumerable<ClientAccountInclude> AccountIncludes { get; set; }
		public IEnumerable<AccountFiedlds> EditAccountFields { get; set; }
	}
}