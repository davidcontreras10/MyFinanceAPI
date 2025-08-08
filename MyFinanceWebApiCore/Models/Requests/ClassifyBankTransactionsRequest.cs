using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class ClassifyBankTransactionsRequest
	{
		[Required]
		[MinLength(1)]
		public IReadOnlyCollection<string> BankTrxIds { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "FinancialEntityId must be greater than 0.")]
		[Required]
		public int FinancialEntityId { get; set; }
	}
}
