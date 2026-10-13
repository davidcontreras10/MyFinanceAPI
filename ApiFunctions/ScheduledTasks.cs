using System;
using System.Threading.Tasks;
using ApiFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ApiFunctions
{
	public class ScheduledTasks(IScheduledTasksService scheduledTasksService, ILogger<ScheduledTasks> logger)
	{
		// ReSharper disable UnusedMember.Local
		private const string TestCron = "0 */2 * * * *";
		private const string DailyCron = "0 10 6 * * *";
		private const string WeeklyCron = "0 0 3 * * 1";
		// ReSharper restore UnusedMember.Local

		[Function(nameof(ScheduledTasks))]
		public async Task Run([TimerTrigger(DailyCron)] TimerInfo myTimer)
		{
			logger.LogInformation($"C# ScheduledTasks executing at: {DateTime.Now}");
			try
			{
				await scheduledTasksService.ExecuteAllTasksAsync();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "LogError ScheduledTasks");
			}
			logger.LogInformation($"C# ScheduledTasks executed at: {DateTime.Now}");
		}
	}
}
