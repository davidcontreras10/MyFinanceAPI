using System;

namespace MyFinanceModel
{
    public sealed record ScheduledDiscoveryResult(int AddedOccurrences, DateTime NextScanUtc, bool HasMore);
    public sealed record PendingScheduledOccurrence(Guid Id, Guid TaskId, Guid OwnerId,
        DateTime ScheduledLocalDate, DateTime DueAtUtc);
}
