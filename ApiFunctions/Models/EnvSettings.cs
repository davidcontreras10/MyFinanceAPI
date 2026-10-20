using System;

namespace ApiFunctions.Models
{
	public interface IEnvSettings
	{
		string BaseAPIUrl { get; }
		string AzureAppUser { get; }
		string AzureAppPw { get; }
		bool UseCoreVersion { get; }
		bool ScheduledTasksV2Enabled { get; }
	}

	public class EnvSettings : IEnvSettings
	{
		private static string BaseNetAPIUrl => Environment.GetEnvironmentVariable("webApiBaseApi");
		private static string BaseCoreAPIUrl => Environment.GetEnvironmentVariable("webApiCoreBaseApi");
		public bool UseCoreVersion => bool.TryParse(Environment.GetEnvironmentVariable("useCoreVersion"), out var version) && version;
		public bool ScheduledTasksV2Enabled => bool.TryParse(Environment.GetEnvironmentVariable("scheduledTasksV2Enabled"), out var enabled) && enabled;
		public string BaseAPIUrl => UseCoreVersion ? BaseCoreAPIUrl : BaseNetAPIUrl;
		public string AzureAppUser => Environment.GetEnvironmentVariable("azureAppUser");
		public string AzureAppPw => Environment.GetEnvironmentVariable("azureAppPw");
	}
}
