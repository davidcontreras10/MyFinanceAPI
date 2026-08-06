using MyFinanceModel.Enums;
using System;

namespace MyFinanceWebApiCore.Models.Requests
{
	public class UpdateDebtorStatusReq
	{
		public DebtorRequestStatus Status { get; set; }
		public DateTime DateTime { get; set; }
	}
}
