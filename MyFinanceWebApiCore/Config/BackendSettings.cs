using Microsoft.Extensions.Options;
using MyFinanceBackend.Models;
using static MyFinanceWebApiCore.Config.AppSettings;

namespace MyFinanceWebApiCore.Config
{
	public class BackendSettings(IOptions<ServicesUrls> servicesUrls, IOptions<OpenAISettings> openAISettings) : IBackendSettings
	{
		private readonly ServicesUrls _servicesUrls = servicesUrls.Value;

		public string CurrencyServiceUrl => _servicesUrls.CurrencyServiceUrl;

		public OpenAISettings OpenAISettings => openAISettings.Value;
	}
}
