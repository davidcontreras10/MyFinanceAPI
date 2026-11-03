using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
    public sealed record ScheduledTaskDispatchFailure(Guid TaskId, string Stage, string Error);

    public sealed record ScheduledTaskDispatchResult(
        int SchedulesScanned,
        int OccurrencesAdded,
        int ExecutionsCompleted,
        int AlreadyCompleted,
        int FailedCount,
        bool HasMoreDiscovery,
        bool HasMoreDueWork,
        IReadOnlyList<ScheduledTaskDispatchFailure> Failures);
}
