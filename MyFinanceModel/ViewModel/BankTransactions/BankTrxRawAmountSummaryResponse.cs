using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;

namespace MyFinanceModel.ViewModel.BankTransactions
{
	public class BankTrxRawAmountSummaryResponse
	{
		public IReadOnlyCollection<BasicCurrencyViewModel> Currencies { get; set; } = Array.Empty<BasicCurrencyViewModel>();
		public IReadOnlyCollection<BankTrxRawAmountSummaryBank> Banks { get; set; } = Array.Empty<BankTrxRawAmountSummaryBank>();
	}

	public class BankTrxRawAmountSummaryBank
	{
		public int FinancialEntityId { get; set; }
		public string FinancialEntityName { get; set; }
		public IReadOnlyCollection<BankTrxSpendSummaryCurrencyAmount> CurrencyAmounts { get; set; } = Array.Empty<BankTrxSpendSummaryCurrencyAmount>();
	}
}
