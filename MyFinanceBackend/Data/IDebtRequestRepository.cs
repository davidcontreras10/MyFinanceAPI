using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IDebtRequestRepository
    {
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<DebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestsByIdAsync(int debtRequestId);
		Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestsByUserAsync(Guid userId);

	}
}
