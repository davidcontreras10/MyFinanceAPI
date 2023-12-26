using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
    public interface IAuthorizationDataRepository
    {
        Task<IEnumerable<UserAssignedAccess>> GetUserAssignedAccessAsync(string userId,
            ApplicationResources applicationResource = ApplicationResources.Unknown,
            ResourceActionNames actionName = ResourceActionNames.Unknown);
    }
}