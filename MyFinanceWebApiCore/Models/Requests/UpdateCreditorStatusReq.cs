using MyFinanceModel.Enums;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class UpdateCreditorStatusReq
	{
		public CreditorRequestStatus Status { get; set; }
	}
}
