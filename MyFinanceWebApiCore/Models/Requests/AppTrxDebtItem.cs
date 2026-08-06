using System.ComponentModel.DataAnnotations;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class AppTrxDebtItem
	{
		[Required]
		[Range(0.00001, float.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
		public float Amount { get; set; }

		[Range(1, int.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
		[Required]
		public int AccountId { get; set; }

		[Required]
		[MaxLength(255)]
		public string Description { get; set; }

		[Required]
		[Range(1, int.MaxValue, ErrorMessage = "The field {0} must be greater than {1}.")]
		public int TrxTypeId { get; set; }
	}
}
