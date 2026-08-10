using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxSpendSummaryResponse
	{
		public IReadOnlyCollection<BasicCurrencyViewModel> Currencies { get; set; } = Array.Empty<BasicCurrencyViewModel>();
		public IReadOnlyCollection<BankTrxSpendSummaryBank> Banks { get; set; } = Array.Empty<BankTrxSpendSummaryBank>();
	}

	public class BankTrxSpendSummaryBank
	{
		public int FinancialEntityId { get; set; }
		public string FinancialEntityName { get; set; }
		public IReadOnlyCollection<BankTrxSpendSummaryAccount> Accounts { get; set; } = Array.Empty<BankTrxSpendSummaryAccount>();
	}

	public class BankTrxSpendSummaryAccount
	{
		public int AccountId { get; set; }
		public string AccountName { get; set; }
		public int CurrencyId { get; set; }
		public IReadOnlyCollection<BankTrxSpendSummaryCurrencyAmount> CurrencyAmounts { get; set; } = Array.Empty<BankTrxSpendSummaryCurrencyAmount>();
		public double Total { get; set; }
	}

	public class BankTrxSpendSummaryCurrencyAmount
	{
		public int CurrencyId { get; set; }
		public double Amount { get; set; }
	}
}
