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
		Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status);
		Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status);
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestsByIdAsync(int debtRequestId);
		Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId);

	}
}
