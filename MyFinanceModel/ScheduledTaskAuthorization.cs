using System;

namespace MyFinanceModel
{
    public sealed record ScheduledTaskCreateScope
    {
        public Guid OwnerId { get; }

        public ScheduledTaskCreateScope(Guid ownerId)
        {
            if (ownerId == Guid.Empty) throw new ArgumentException("A task creation scope requires an owner.");
            OwnerId = ownerId;
        }
    }

    public sealed record ScheduledTaskCreationAuthorizationResult(ScheduledTaskCreateScope Scope)
    {
        public bool IsAuthorized => Scope != null;
    }

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
