using System;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public sealed class ScheduledTaskAuthorizationService(IScheduledTaskAccessRepository tasks,
        IAccountAccessRepository accounts) : IScheduledTaskAuthorizationService
    {
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
