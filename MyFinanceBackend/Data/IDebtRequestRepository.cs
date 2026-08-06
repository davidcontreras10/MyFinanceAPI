using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IDebtRequestRepository
    {
		Task RemoveAllAppTransactionsFromDebtRequestAsync(int debtRequestId, Guid userId);
		Task AddAppTransactionAsync(int debtRequestId, IReadOnlyCollection<int> trxIds, Guid userId);
		Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status);
		Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status);
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<DebtRequestVm> GetDebtRequestsByIdAsync(int debtRequestId, Guid? userId = null, bool includeAppTrxs = false);
		Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId, bool includeAppTrxs = false);
		Task<T> GetDebtRequestsByIdAsync<T>(int debtRequestId, Guid? userId = null, bool includeAppTrxs = false) where T : DebtRequestVm;

    }
}
