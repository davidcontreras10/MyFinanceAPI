using MyFinanceModel.Enums;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class UpdateDebtorStatusReq
	{
		public DebtorRequestStatus Status { get; set; }
	}
}
