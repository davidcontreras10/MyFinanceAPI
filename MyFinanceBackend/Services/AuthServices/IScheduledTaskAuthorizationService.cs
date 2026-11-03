using System.Threading.Tasks;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Services.AuthServices
{
    public interface IScheduledTaskAuthorizationService
    {
        Task<ScheduledTaskAccessAuthorizationResult> AuthorizeAccessAsync(string authenticatedUserId,
            string requestedTaskId, ResourceActionNames action);
        Task<ScheduledTaskExecutionAuthorizationResult> AuthorizeExecutionAsync(string authenticatedUserId,
            string requestedTaskId, bool scheduledOccurrence);
        Task<ScheduledTaskCreationAuthorizationResult> AuthorizeCreateAsync(string authenticatedUserId, ClientScheduledTask request);
        Task<ScheduledTaskAuthorizationResult> AuthorizeEditAsync(string authenticatedUserId,
            string requestedTaskId, int? requestedSpendTypeId = null);
    }
}
