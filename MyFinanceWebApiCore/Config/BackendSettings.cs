using Microsoft.Extensions.Options;
using MyFinanceBackend.Models;
using static MyFinanceWebApiCore.Config.AppSettings;

namespace MyFinanceWebApiCore.Config
{
	public class BackendSettings : IBackendSettings
	{
		private readonly ServicesUrls _servicesUrls;

		public BackendSettings(IOptions<ServicesUrls> servicesUrls)
		{
			_servicesUrls = servicesUrls.Value;
		}

		public string CurrencyServiceUrl => _servicesUrls.CurrencyServiceUrl;
	}
}
