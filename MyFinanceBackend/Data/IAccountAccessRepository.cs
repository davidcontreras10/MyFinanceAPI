using System;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
    public interface IAccountAccessRepository
    {
        Task<bool> IsWithinOwnerScopeAsync(Guid ownerId, AccountAccessTargets targets);
    }
}
