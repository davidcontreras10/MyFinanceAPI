using System;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
    public interface IScheduledExecutionRepository
    {
        Task<ScheduledExecutionAttemptHistoryPage> GetAttemptHistoryAsync(ScheduledTaskAccessScope scope,
            int skip, int limit, CancellationToken cancellationToken = default);

        // The callback must write through repositories sharing this scoped unit of work.
        // A callback failure rolls back both its financial changes and completion.
        Task<ScheduledExecutionResult> ExecuteAsync(
            ScheduledExecutionRequest request,
            Func<ScheduledExecutionWork, CancellationToken, Task> execute,
            CancellationToken cancellationToken = default);
    }
}
