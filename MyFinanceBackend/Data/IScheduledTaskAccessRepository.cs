using System;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
    public interface IScheduledTaskAccessRepository
    {
        Task<bool> IsOwnedByAsync(Guid taskId, Guid ownerId);
    }
}
