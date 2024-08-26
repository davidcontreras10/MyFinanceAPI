using EFDataAccess.Models;
using MyFinanceBackend.Data;
using System;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFAppTransferRepository(MyFinanceContext context) : BaseEFRepository(context), IAppTransferRepository
	{
		public async Task AddAsync(int sourceTrxId, int destinationTrxId)
		{
			await Context.AppTransfers.AddAsync(new EFAppTransfer
			{
				SourceAppTrxId = sourceTrxId,
				DestinationAppTrxId = destinationTrxId
			});

			await Context.SaveChangesAsync();
		}
	}
}
