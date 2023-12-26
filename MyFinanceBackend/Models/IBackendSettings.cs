using System;
using System.Collections.Generic;
using System.Text;

namespace MyFinanceBackend.Models
{
	public interface IBackendSettings
	{
		string CurrencyServiceUrl { get; }
	}
}
