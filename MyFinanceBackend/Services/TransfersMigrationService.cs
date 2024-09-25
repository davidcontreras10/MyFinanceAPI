using MyFinanceBackend.Data;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface ITransfersMigrationService
	{
		Task MigrateTransfersAsync();
	}

	public class TransfersMigrationService(IUnitOfWork unitOfWork) : ITransfersMigrationService
	{
		public async Task MigrateTransfersAsync()
		{
			await unitOfWork.TransferRepository.ExecuteMigrationAsync();
		}
	}
}
