using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IDebtRequestService
	{
		Task RemoveAppTransactionsFromDebtRequestAsync(int debtRequestId, Guid userId);
		Task<UserDebtRequestVm> GetDebtRequestByIdAsync(int debtRequestId, Guid userId);
		Task<IReadOnlyCollection<TrxItemModifiedRecord>> AddAppTransactionsToDebtRequestAsync(
			Guid userId,
			int debtRequestId,
			IReadOnlyCollection<NewDebtRequestAppTrx> newDebtRequestAppTrxes
			);
		Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status, Guid userId, DateTime dateTime);
		Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status, Guid userId, DateTime dateTime);
		Task DeleteDebtRequestAsync(int debtRequestId);
		Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId);
		Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest simpleDebtRequest);
		Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestByUserIdAsync(Guid userId);
	}
}
