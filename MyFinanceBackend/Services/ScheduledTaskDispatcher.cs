using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
    public sealed class ScheduledTaskDispatchOptions
    {
        public bool Enabled { get; init; }
    }

    public interface IScheduledTaskDispatcher
    {
        Task<ScheduledTaskDispatchResult> DispatchDueAsync(Guid administratorId,
            CancellationToken cancellationToken = default);
    }

    public sealed class ScheduledTaskDispatcher(IScheduledScheduleRepository schedules,
        IScheduledTaskExecutionService executions, ScheduledExecutionOptions executionOptions,
        ScheduledTaskDispatchOptions dispatchOptions, ILogger<ScheduledTaskDispatcher> logger)
        : IScheduledTaskDispatcher
    {
        private const int ScanBatchSize = 50;
        private const int ExecutionBatchSize = 50;

        public async Task<ScheduledTaskDispatchResult> DispatchDueAsync(Guid administratorId,
            CancellationToken cancellationToken = default)
        {
            if (administratorId == Guid.Empty)
                throw new ServiceException("An authenticated administrator is required.", HttpStatusCode.Unauthorized);
            if (!dispatchOptions.Enabled)
                throw new ServiceException("Scheduled-task dispatch is not enabled.", HttpStatusCode.ServiceUnavailable);
            if (!executionOptions.HistoryEnabled)
                throw new ServiceException("Schedule history is not enabled.", HttpStatusCode.ServiceUnavailable);
            if (!executionOptions.Enabled)
                throw new ServiceException("The new scheduled execution path is not enabled.", HttpStatusCode.ServiceUnavailable);

            // Repository methods validate persisted Admin membership, not only token claims.
            var scheduleIds = await schedules.GetSchedulesToScanAsync(administratorId, ScanBatchSize, cancellationToken);
            var failures = new List<ScheduledTaskDispatchFailure>();
            var occurrencesAdded = 0;
            var hasMoreDiscovery = scheduleIds.Count == ScanBatchSize;

            foreach (var taskId in scheduleIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var discovery = await schedules.DiscoverAsync(taskId, administratorId, cancellationToken);
                    occurrencesAdded += discovery.AddedOccurrences;
                    hasMoreDiscovery |= discovery.HasMore;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Scheduled occurrence discovery failed for task {TaskId}.", taskId);
                    failures.Add(new ScheduledTaskDispatchFailure(taskId, "discovery", error.Message));
                }
            }

            var pending = await schedules.GetPendingAsync(administratorId, ExecutionBatchSize, cancellationToken);
            var hasMoreDueWork = pending.Count == ExecutionBatchSize;
            var completed = 0;
            var alreadyCompleted = 0;

            foreach (var occurrence in pending)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var scope = new ScheduledTaskExecutionScope(administratorId, occurrence.OwnerId,
                        occurrence.TaskId, administratorDispatch: true);
                    var request = new ScheduledExecutionRequest(occurrence.TaskId, administratorId,
                        occurrence.Id, null, null);
                    var result = await executions.ExecuteAsync(request, scope, cancellationToken);
                    if (result.AlreadyCompleted) alreadyCompleted++;
                    else completed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception error)
                {
                    logger.LogError(error, "Scheduled occurrence execution failed for task {TaskId}, occurrence {OccurrenceId}.",
                        occurrence.TaskId, occurrence.Id);
                    failures.Add(new ScheduledTaskDispatchFailure(occurrence.TaskId, "execution", error.Message));
                }
            }

            return new ScheduledTaskDispatchResult(scheduleIds.Count, occurrencesAdded, completed, alreadyCompleted,
                failures.Count, hasMoreDiscovery, hasMoreDueWork, failures);
        }
    }
}
