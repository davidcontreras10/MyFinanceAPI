using System;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public sealed class ScheduledTaskAuthorizationService(IScheduledTaskAccessRepository tasks,
        IAccountAccessRepository accounts) : IScheduledTaskAuthorizationService
    {
        public async Task<ScheduledTaskAccessAuthorizationResult> AuthorizeAccessAsync(string authenticatedUserId,
            string requestedTaskId, ResourceActionNames action)
        {
            if (!Guid.TryParse(authenticatedUserId, out var owner) || owner == Guid.Empty ||
                !Guid.TryParse(requestedTaskId, out var task) || task == Guid.Empty ||
                action is not (ResourceActionNames.View or ResourceActionNames.Delete) ||
                !await tasks.IsOwnedByAsync(task, owner))
                return new ScheduledTaskAccessAuthorizationResult(null);
            return new ScheduledTaskAccessAuthorizationResult(new ScheduledTaskAccessScope(owner, task, action));
        }

        public async Task<ScheduledTaskExecutionAuthorizationResult> AuthorizeExecutionAsync(string authenticatedUserId,
            string requestedTaskId, bool scheduledOccurrence)
        {
            if (!Guid.TryParse(authenticatedUserId, out var actor) || actor == Guid.Empty ||
                !Guid.TryParse(requestedTaskId, out var task) || task == Guid.Empty)
                return new ScheduledTaskExecutionAuthorizationResult(null);
            var owner = await tasks.GetOwnerIdAsync(task);
            if (!owner.HasValue || owner == Guid.Empty) return new ScheduledTaskExecutionAuthorizationResult(null);
            var crossUser = owner.Value != actor;
            if (crossUser && (!scheduledOccurrence || !await tasks.IsAdministratorAsync(actor)))
                return new ScheduledTaskExecutionAuthorizationResult(null);
            return new ScheduledTaskExecutionAuthorizationResult(new ScheduledTaskExecutionScope(actor, owner.Value, task, crossUser));
        }

        public async Task<ScheduledTaskCreationAuthorizationResult> AuthorizeCreateAsync(string authenticatedUserId,
            ClientScheduledTask request)
        {
            if (!Guid.TryParse(authenticatedUserId, out var ownerId) || ownerId == Guid.Empty ||
                request is not (ClientScheduledTask.Basic or ClientScheduledTask.Transfer))
                return new ScheduledTaskCreationAuthorizationResult(null);
            var ids = request is ClientScheduledTask.Transfer transfer
                ? new[] { request.AccountId, transfer.ToAccountId }.Distinct().ToArray() : new[] { request.AccountId };
            if (ids.Any(id => id <= 0) || request.SpendTypeId <= 0 ||
                !await accounts.IsWithinOwnerScopeAsync(ownerId,
                    new AccountAccessTargets { AccountIds = ids, SpendTypeIds = new[] { request.SpendTypeId } }))
                return new ScheduledTaskCreationAuthorizationResult(null);
            return new ScheduledTaskCreationAuthorizationResult(new ScheduledTaskCreateScope(ownerId));
        }

        public async Task<ScheduledTaskAuthorizationResult> AuthorizeEditAsync(string authenticatedUserId,
            string requestedTaskId, int? requestedSpendTypeId = null)
        {
            if (!Guid.TryParse(authenticatedUserId, out var ownerId) || ownerId == Guid.Empty ||
                !Guid.TryParse(requestedTaskId, out var taskId) || taskId == Guid.Empty ||
                requestedSpendTypeId is <= 0)
                return new ScheduledTaskAuthorizationResult(null);

            if (!await tasks.IsOwnedByAsync(taskId, ownerId) ||
                (requestedSpendTypeId.HasValue && !await accounts.IsWithinOwnerScopeAsync(ownerId,
                    new AccountAccessTargets { SpendTypeIds = new[] { requestedSpendTypeId.Value } })))
                return new ScheduledTaskAuthorizationResult(null);

            return new ScheduledTaskAuthorizationResult(new ScheduledTaskEditScope(ownerId, taskId));
        }
    }
}
