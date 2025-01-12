using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFDebtRequestRepository(MyFinanceContext context) : BaseEFRepository(context), IDebtRequestRepository
	{
		public async Task<DebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest)
		{
			var debtRequest = new EFDebtRequest
			{
				EventName = simpleDebtRequest.EventName,
				EventDescription = simpleDebtRequest.EventDescription,
				EventDate = simpleDebtRequest.EventDate,
				Amount = simpleDebtRequest.Amount,
				CurrencyId = simpleDebtRequest.CurrencyId,
				CreditorId = simpleDebtRequest.CreditorId,
				DebtorId = simpleDebtRequest.DebtorId
			};
			await Context.DebtRequests.AddAsync(debtRequest);
			await Context.SaveChangesAsync();
			await Context.Entry(debtRequest).Reference(x => x.Currency).LoadAsync();
			await Context.Entry(debtRequest).Reference(x => x.CreditorUser).LoadAsync();
			await Context.Entry(debtRequest).Reference(x => x.DebtorUser).LoadAsync();
			return debtRequest.ToDebtRequestVm();
		}

		public async Task DeleteDebtRequestAsync(int debtRequestId)
		{
			var debtRequest = await Context.DebtRequests.FindAsync(debtRequestId);
			if (debtRequest == null)
			{
				throw new Exception("Debt request not found");
			}
			
			Context.DebtRequests.Remove(debtRequest);
			await Context.SaveChangesAsync();
		}

		public async Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestsByIdAsync(int debtRequestId)
		{
			var debtRequests = await Context.DebtRequests.AsNoTracking()
				.Where(x => x.Id == debtRequestId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.ToListAsync();
			
			return debtRequests.Select(x => x.ToDebtRequestVm()).ToList();
		}

		public async Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId)
		{
			var debtRequests = await Context.DebtRequests.AsNoTracking()
				.Where(x => x.CreditorId == userId || x.DebtorId == userId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.ToListAsync();

			return debtRequests.Select(x => x.ToDebtRequestVm()).ToList();
		}
	}
}
