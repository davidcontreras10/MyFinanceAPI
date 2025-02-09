using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IDebtRequestService
	{
		Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status);
		Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status);
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId);
		Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestByUserIdAsync(Guid userId);
	}
}
