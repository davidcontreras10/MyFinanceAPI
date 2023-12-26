using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public interface IUserAuthorizeService
    {
        Task<bool> IsAuthorizedAsync(string authenticatedUserId, IEnumerable<string> targetUserIds,
            IEnumerable<ResourceActionNames> actionNames);
    }
}
