using System;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
    public interface IScheduledUserPreferencesRepository
    {
        Task<ScheduledUserPreferences> GetPreferencesAsync(Guid ownerId, CancellationToken cancellationToken = default);
        Task<ScheduledUserPreferences> UpdatePreferencesAsync(Guid ownerId, string timeZoneId,
            CancellationToken cancellationToken = default);
    }
}
