using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IAppTransferRepository
    {
        Task AddAsync(int sourceTrxId, int destinationTrxId);
	}
}
