using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceBackend.Attributes;

namespace MyFinanceBackend.Services
{
    public interface IAuthorizationService
    {
        Task<bool> IsAuthorizedAsync(string authenticatedUserId, IEnumerable<string> targetUserIds,
            ResourceActionRequiredAttribute resourceActionRequiredAttribute);
    }
}