using System;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public class AccountAuthorizationService(IAccountAccessRepository repository) : IAccountAuthorizationService
    {
        public async Task<AccountAuthorizationResult> AuthorizeAsync(string authenticatedUserId,
            ResourceActionNames action, AccountAccessTargets targets)
        {
            if (!Guid.TryParse(authenticatedUserId, out var ownerId) || ownerId == Guid.Empty ||
                action is not (ResourceActionNames.View or ResourceActionNames.Add or ResourceActionNames.Edit or ResourceActionNames.Delete) ||
                targets == null ||
                !ValidIds(targets.AccountIds) || !ValidIds(targets.AccountPeriodIds) ||
                !ValidIds(targets.AccountGroupIds) || !ValidIds(targets.SpendTypeIds))
                return new AccountAuthorizationResult(null);

            // Owner-only policy. Future grants belong here, keeping identity out of operation services.
            if (!await repository.IsWithinOwnerScopeAsync(ownerId, targets))
                return new AccountAuthorizationResult(null);

            return new AccountAuthorizationResult(new AccountAccessScope(ownerId));
        }

        private static bool ValidIds(System.Collections.Generic.IReadOnlyCollection<int> ids) =>
            ids != null && ids.All(id => id > 0);
    }
}
