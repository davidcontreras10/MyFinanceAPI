using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class DebtRequestService(IUnitOfWork unitOfWork) : IDebtRequestService
	{
		public async Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus status)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.UpdateCreditorStatusAsync(debtRequestId, status);
			return debtRequest;
		}

		public async Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.UpdateDebtorStatusAsync(debtRequestId, status);
			return debtRequest;
		}

		public async Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId)
		{
			var currencies = await unitOfWork.CurrenciesRepository.GetCurrenciesAsync();
			var users = await unitOfWork.UserRepository.GetAppUsersAsync();
			var usersVm = users.Where(x => x.UserId != userId && x.HasRole(RoleId.User)).Select(x =>
			{
				return new BasicUserViewModel(x.UserId, x.Username, x.Name);
			}).ToList();

			return new CreateSimpleDebtRequestVm(currencies, usersVm);
		}

		public async Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest clientDebtRequest)
		{
			return await unitOfWork.DebtRequestRepository.CreateSimpleDebtRequestAsync(clientDebtRequest);
		}

		public async Task DeleteDebtRequestAsync(int debtRequestId)
		{
			await unitOfWork.DebtRequestRepository.DeleteDebtRequestAsync(debtRequestId);
		}

		public async Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestByUserIdAsync(Guid userId)
		{
			return await unitOfWork.DebtRequestRepository.GetDebtRequestsByUserAsync(userId);
		}
	}
}
