using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public interface IAccountAuthorizationService
    {
        Task<AccountAuthorizationResult> AuthorizeAsync(string authenticatedUserId,
            ResourceActionNames action, AccountAccessTargets targets);
    }
}
