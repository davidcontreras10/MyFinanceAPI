using MyFinanceModel.BankTrxCategorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyFinanceBackend.Utils
{
	public static class ExpenseDataCleanup<T> where T : IExpenseItem
	{
		public static IReadOnlyCollection<T> Clean(IReadOnlyCollection<T> input, decimal tolerancePercent = 20)
		{
			// Apply normalization and deduplication pipeline
			input.ApplyDescriptionNormalizations();
			var cleaned = Deduplicate(input, tolerancePercent);
			// Add future cleanup steps here

			return cleaned;
		}

		private static IReadOnlyCollection<T> Deduplicate(IReadOnlyCollection<T> input, decimal tolerancePercent)
		{
			var result = new List<T>();
			foreach (var item in input)
			{
				var match = result.FirstOrDefault(existing =>
					existing.Description == item.Description &&
					existing.AccountName == item.AccountName &&
					existing.Category == item.Category &&
					existing.Currency == item.Currency &&
					AreAmountsSimilar(existing.Amount, item.Amount, tolerancePercent));

				if (match == null)
				{
					result.Add(item);
				}
			}

			return result;
		}

		private static bool AreAmountsSimilar(decimal a, decimal b, decimal tolerancePercent)
		{
			if (a == 0 || b == 0) return false;
			var percentDiff = Math.Abs(a - b) / Math.Max(a, b);
			return percentDiff <= (tolerancePercent / 100.0m);
		}
	}

	public static class ExpenseDataExtensions
	{
		private static readonly List<(Regex Pattern, string Replacement)> NormalizationRules = new()
		{
			(new Regex(@"GOOGLE \*CLOUD [A-Z0-9]+", RegexOptions.IgnoreCase), "GOOGLE *CLOUD <id>"),
			(new Regex(@"MICROSOFT#G\d+", RegexOptions.IgnoreCase), "MICROSOFT#G<ID>")
			// Add more patterns here as needed
		};


		public static void ApplyDescriptionNormalizations<TD>(this IReadOnlyCollection<TD> input) where TD : IWithDescription
		{
			foreach (var item in input)
			{
				item.Description = NormalizeDescription(item.Description);
			}
		}

		public static string NormalizeDescription(string description)
		{
			foreach (var (pattern, replacement) in NormalizationRules)
			{
				if (pattern.IsMatch(description))
					description = pattern.Replace(description, replacement);
			}

			return description.Trim();
		}
	}
}
