using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFDebtRequestRepository(MyFinanceContext context) : BaseEFRepository(context), IDebtRequestRepository
	{
		public async Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status)
		{
			var debtRequest = Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.Single();

			debtRequest.CreditorStatus = status;
			await Context.SaveChangesAsync();
			return debtRequest.ToDebtRequestVm<UserDebtRequestVm>(debtRequest.CreditorId);
		}

		public async Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status)
		{
			var debtRequest = Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.Single();

			debtRequest.DebtorStatus = status;
			await Context.SaveChangesAsync();
			return debtRequest.ToDebtRequestVm<UserDebtRequestVm>(debtRequest.DebtorId);
		}

		public async Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest)
		{
			var debtRequest = new EFDebtRequest
			{
				EventName = simpleDebtRequest.EventName,
				EventDescription = simpleDebtRequest.EventDescription,
				EventDate = simpleDebtRequest.EventDate,
				Amount = simpleDebtRequest.Amount,
				CurrencyId = simpleDebtRequest.CurrencyId,
				CreditorId = simpleDebtRequest.CreditorId,
				DebtorId = simpleDebtRequest.DebtorId,
				CreditorStatus = CreditorRequestStatus.Pending,
				DebtorStatus = DebtorRequestStatus.Pending,
				CreatedDate = DateTime.UtcNow
			};
			await Context.DebtRequests.AddAsync(debtRequest);
			await Context.SaveChangesAsync();
			await Context.Entry(debtRequest).Reference(x => x.Currency).LoadAsync();
			await Context.Entry(debtRequest).Reference(x => x.CreditorUser).LoadAsync();
			await Context.Entry(debtRequest).Reference(x => x.DebtorUser).LoadAsync();
			return debtRequest.ToDebtRequestVm<UserDebtRequestVm>(simpleDebtRequest.CreditorId);
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
			
			return debtRequests.Select(x => x.ToDebtRequestVm<DebtRequestVm>()).ToList();
		}

		public async Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId)
		{
			var debtRequests = await Context.DebtRequests.AsNoTracking()
				.Where(x => x.CreditorId == userId || x.DebtorId == userId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.ToListAsync();

			return debtRequests.Select(x => x.ToDebtRequestVm<UserDebtRequestVm>(userId)).ToList();
		}
	}
}
