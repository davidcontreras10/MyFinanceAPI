using System;

namespace MyFinanceWebApiCore.Config
{
	public class AppSettings
	{
		public class ServicesUrls
		{
			public string CurrencyServiceUrl { get; set; }
		}

		public class AuthConfig
		{
			public string Secret { get; set; }
			public TimeSpan	TokenExpiresIn { get; set; }
		}
	}
}
