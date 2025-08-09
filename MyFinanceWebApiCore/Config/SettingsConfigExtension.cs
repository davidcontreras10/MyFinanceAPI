using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFinanceBackend.Models;

namespace MyFinanceWebApiCore.Config
{
	public static class SettingsConfigExtension
	{
		public static void ConfigureSettings(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<AppSettings.AuthConfig>(configuration.GetSection("authentication"));
			services.Configure<AppSettings.ServicesUrls>(configuration.GetSection("servicesUrls"));
			services.Configure<OpenAISettings>(configuration.GetSection("OpenAI"));
		}
	}
}
