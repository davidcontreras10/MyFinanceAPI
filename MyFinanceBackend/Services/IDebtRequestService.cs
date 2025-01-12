using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IDebtRequestService
	{
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId);
		Task<DebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<IReadOnlyCollection<DebtRequestVm>> GetDebtRequestByUserIdAsync(Guid userId);
	}
}
