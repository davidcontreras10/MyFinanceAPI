using EFDataAccess.Extensions;
using EFDataAccess.Helpers;
using EFDataAccess.Models;
using EFDataAccess.Models.Customs;
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
		public async Task<IReadOnlyCollection<int>> GetAppTransactionIdsByDebtRequestIdAsync(int debtRequestId, Guid userId)
		{
			var debtRequestAdditional = await Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.IncludeAllAdditionalQuery(true)
				.FirstOrDefaultAsync();
			var debtRequest = debtRequestAdditional.EFDebtRequest;
			if (debtRequest == null)
			{
				throw new Exception("Debt request not found");
			}
			bool? isCreditor = null;
			if (debtRequest.CreditorId == userId)
			{
				isCreditor = true;
			}
			else if (debtRequest.DebtorId == userId)
			{
				isCreditor = false;
			}
			else
			{
				throw new Exception("User is not part of the debt request");
			}
			var spendCollection = debtRequest.GetSpendCollection(isCreditor.Value);
			return spendCollection.Select(x => x.SpendId).ToList();
		}

		public async Task RemoveAllAppTransactionsFromDebtRequestAsync(int debtRequestId, Guid userId)
		{
			var debtRequest = await Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.Include(x => x.CreditorSpends)
				.Include(x => x.DebtorSpends)
				.FirstOrDefaultAsync();
			if (debtRequest == null)
			{
				throw new Exception("Debt request not found");
			}
			bool? isCreditor = null;
			if (debtRequest.CreditorId == userId)
			{
				isCreditor = true;
			}
			else if (debtRequest.DebtorId == userId)
			{
				isCreditor = false;
			}
			else
			{
				throw new Exception("User is not part of the debt request");
			}
			var spendCollection = debtRequest.GetSpendCollection(isCreditor.Value);
			if (spendCollection.Count == 0)
			{
				throw new Exception("No transactions to remove");
			}

			Context.Spend.RemoveRange(spendCollection);
			Context.SpendOnPeriod.RemoveRange(Context.SpendOnPeriod.Where(x => spendCollection.Select(s => s.SpendId).Contains(x.SpendId)));
			debtRequest.SetSpendCollection(isCreditor.Value, []);
		}

		public async Task AddAppTransactionAsync(int debtRequestId, IReadOnlyCollection<int> trxIds, Guid userId)
		{
			var existingAppTrx = await Context.Spend.Where(x => trxIds.Contains(x.SpendId)).ToListAsync();
			if (existingAppTrx.Count != trxIds.Count)
			{
				throw new Exception("One or more transactions not found");
			}

			bool? isCreditor = null;
			var debtRequest = await Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser)
				.FirstOrDefaultAsync();

			if (debtRequest.CreditorId == userId)
			{
				isCreditor = true;
			}
			else if (debtRequest.DebtorId == userId)
			{
				isCreditor = false;
			}
			else
			{
				throw new Exception("User is not part of the debt request");
			}

			if(debtRequest.GetSpendCollection(isCreditor.Value).Count != 0)
			{
				throw new Exception("Transactions already added to this debt request");
			}
			var spendsToAdd = Context.Spend.Where(x => trxIds.Contains(x.SpendId)).ToList();
			debtRequest.SetSpendCollection(isCreditor.Value, spendsToAdd);
		}

		public async Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status)
		{
			var debtRequest = await Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.IncludeAllAdditionalQuery(false)
				.FirstOrDefaultAsync();

			debtRequest.EFDebtRequest.CreditorStatus = status;
			return debtRequest.ToDebtRequestVm<UserDebtRequestVm>(debtRequest.EFDebtRequest.CreditorId);
		}

		public async Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status)
		{
			var debtRequest = await Context.DebtRequests
				.Where(x => x.Id == debtRequestId)
				.IncludeAllAdditionalQuery(false)
				.FirstOrDefaultAsync();

			debtRequest.EFDebtRequest.DebtorStatus = status;
			return debtRequest.ToDebtRequestVm<UserDebtRequestVm>(debtRequest.EFDebtRequest.DebtorId);
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
			var efDebtRequestAdditional = new EFDebtRequestAdditional
			{
				EFDebtRequest = debtRequest,
				CreditorSpendsCount = 0,
				DebtorSpendsCount = 0
			};
			return efDebtRequestAdditional.ToDebtRequestVm<UserDebtRequestVm>(simpleDebtRequest.CreditorId);
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

		public async Task<DebtRequestVm> GetDebtRequestsByIdAsync(int debtRequestId, Guid? userId = null, bool includeAppTrxs = false)
		{
			var debtRequests = await Context.DebtRequests.AsNoTracking()
				.Where(x => x.Id == debtRequestId)
				.IncludeAllAdditionalQuery(includeAppTrxs && userId != null)
				.FirstOrDefaultAsync();

			return userId != null ? debtRequests?.ToDebtRequestVm<UserDebtRequestVm>(userId.Value)
				: debtRequests?.ToDebtRequestVm<DebtRequestVm>();
		}

		public async Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId, bool includeAppTrxs = false)
		{
			var debtRequests = await Context.DebtRequests.AsNoTracking()
				.Where(x => x.CreditorId == userId || x.DebtorId == userId)
				.IncludeAllAdditionalQuery(includeAppTrxs)
				.ToListAsync();

			return debtRequests.Select(x => x.ToDebtRequestVm<UserDebtRequestVm>(userId)).ToList();
		}

	}
}
