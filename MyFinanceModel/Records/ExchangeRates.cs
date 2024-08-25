using System;
using System.Collections.Generic;

namespace MyFinanceModel.Records
{
	public record class ExchangeRateRequest(IEnumerable<MethodParam> MethodIds, DateTime DateTime);

	public record class MethodParam(int Id, bool IsPurchase, DateTime DateTime);
}
