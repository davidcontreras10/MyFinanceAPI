using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public interface IScheduledTaskAuthorizationService
    {
        Task<ScheduledTaskAuthorizationResult> AuthorizeEditAsync(string authenticatedUserId,
            string requestedTaskId, int? requestedSpendTypeId = null);
    }
}
