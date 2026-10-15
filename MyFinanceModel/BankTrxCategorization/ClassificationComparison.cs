using System.Collections.Generic;

namespace MyFinanceModel.BankTrxCategorization
{
	public class ClassificationComparison
	{
		public IReadOnlyCollection<ExpenseToClassify> Inputs { get; set; }
		public IReadOnlyCollection<ClassificationModelRun> Runs { get; set; }
	}

	public class ClassificationModelRun
	{
		public string RequestedModel { get; set; }
		public string ReturnedModel { get; set; }
		public string ReasoningEffort { get; set; }
		public long ElapsedMilliseconds { get; set; }
		public string RequestId { get; set; }
		public string ServiceTier { get; set; }
		public int? InputTokens { get; set; }
		public int? CachedInputTokens { get; set; }
		public int? CacheWriteTokens { get; set; }
		public int? OutputTokens { get; set; }
		public int? ReasoningTokens { get; set; }
		public decimal InputUsdPerMillionTokens { get; set; }
		public decimal CachedInputUsdPerMillionTokens { get; set; }
		public decimal CacheWriteUsdPerMillionTokens { get; set; }
		public decimal OutputUsdPerMillionTokens { get; set; }
		public string PricingAsOf { get; set; } = "2026-10-03";
		public string PricingSource { get; set; }
		public decimal? EstimatedCostUsd { get; set; }
		public bool Succeeded { get; set; }
		public string Error { get; set; }
		public object ErrorDetails { get; set; }
		public IReadOnlyCollection<OutGptClassifiedExpense> Results { get; set; } = [];
	}
}
