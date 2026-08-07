using MyFinanceModel.Enums;
using System;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class UpdateCreditorStatusReq
	{
		public CreditorRequestStatus Status { get; set; }
		public DateTime DateTime { get; set; }
	}
}
