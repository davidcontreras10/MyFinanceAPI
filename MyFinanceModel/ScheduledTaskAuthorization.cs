using System;

namespace MyFinanceModel
{
    public sealed record ScheduledTaskEditScope
    {
        public Guid OwnerId { get; }
        public Guid TaskId { get; }

        public ScheduledTaskEditScope(Guid ownerId, Guid taskId)
        {
            if (ownerId == Guid.Empty || taskId == Guid.Empty)
                throw new ArgumentException("A scheduled-task edit scope requires an owner and task.");
            OwnerId = ownerId;
            TaskId = taskId;
        }
    }

    public sealed record ScheduledTaskAuthorizationResult(ScheduledTaskEditScope Scope)
    {
        public bool IsAuthorized => Scope != null;
    }
}
