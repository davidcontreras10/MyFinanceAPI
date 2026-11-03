using System;
using System.Threading.Tasks;
using ApiFunctions.Models;
using ApiFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ApiFunctions
{
	public class ScheduledTasksV2(
		IScheduledTasksService scheduledTasksService,
		IEnvSettings envSettings,
		ILogger<ScheduledTasksV2> logger)
	{
		private const string HourlyCron = "0 0 * * * *";

		[Function(nameof(ScheduledTasksV2))]
		public async Task Run([TimerTrigger(HourlyCron)] TimerInfo timer)
		{
			if (!envSettings.ScheduledTasksV2Enabled)
			{
				logger.LogInformation("Scheduled task v2 dispatcher is disabled.");
				return;
			}

			var result = await scheduledTasksService.DispatchDueTasksV2Async();
		if (result == null)
		{
			throw new InvalidOperationException("Scheduled task dispatcher returned no result.");
		}

		foreach (var failure in result.Failures ?? Array.Empty<ScheduledTaskDispatchFailure>())
			{
				logger.LogError("Scheduled task dispatch failed for task {TaskId} at {Stage}: {Error}",
					failure.TaskId, failure.Stage, failure.Error);
			}

			logger.LogInformation(
				"Scheduled task dispatch completed. Scanned {SchedulesScanned} schedules, added {OccurrencesAdded} occurrences, completed {ExecutionsCompleted}, already completed {AlreadyCompleted}, failed {FailedCount}.",
				result.SchedulesScanned, result.OccurrencesAdded, result.ExecutionsCompleted,
				result.AlreadyCompleted, result.FailedCount);

			if (result.HasMoreDiscovery || result.HasMoreDueWork)
			{
				logger.LogWarning("Scheduled task dispatch has more bounded work remaining; it will continue on a later hourly run.");
			}

			if (result.FailedCount > 0)
			{
				throw new InvalidOperationException($"Scheduled task dispatch reported {result.FailedCount} failure(s).");
			}
		}
	}
}
