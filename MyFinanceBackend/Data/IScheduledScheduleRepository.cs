using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
    public interface IScheduledScheduleRepository
    {
        Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(ScheduledTaskAccessScope scope,
            CancellationToken cancellationToken = default);
        Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(ScheduledTaskAccessScope scope, int skip, int limit,
            CancellationToken cancellationToken = default);
        Task RetireAsync(ScheduledTaskAccessScope scope, CancellationToken cancellationToken = default);
        Task<TaskExecutedResult> ExecuteLegacyAsync(ScheduledTaskExecutionScope scope,
            Func<Task<TaskExecutedResult>> execute, CancellationToken cancellationToken = default);
        Task<Guid> RegisterAsync(Guid ownerId, Func<Task<Guid>> createTask,
            CancellationToken cancellationToken = default);
        Task ActivateAsync(Guid taskId, Guid administratorId, DateTimeOffset activationUtc,
            string timeZoneId, CancellationToken cancellationToken = default);
        Task<ScheduledDiscoveryResult> DiscoverAsync(Guid taskId, Guid actorId,
            CancellationToken cancellationToken = default);
        Task ReviseAsync(Guid taskId, Guid ownerId, Func<Task> update,
            string timeZoneId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<PendingScheduledOccurrence>> GetPendingAsync(Guid actorId,
            int limit, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetSchedulesToScanAsync(Guid administratorId,
            int limit, CancellationToken cancellationToken = default);
    }
}
