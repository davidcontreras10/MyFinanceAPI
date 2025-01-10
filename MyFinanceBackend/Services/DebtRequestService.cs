using MyFinanceBackend.Data;
using MyFinanceModel.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class DebtRequestService(IUnitOfWork unitOfWork) : IDebtRequestService
	{
		public async Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId)
		{
			var currencies = await unitOfWork.CurrenciesRepository.GetCurrenciesAsync();
			var users = await unitOfWork.UserRepository.GetAppUsersAsync();
			var usersVm = users.Where(x => x.UserId != userId).Select(x =>
			{
				return new BasicUserViewModel
				{
					UserId = x.UserId,
					Username = x.Username,
					Name = x.Name
				};
			}).ToList();
			return new CreateSimpleDebtRequestVm(currencies, usersVm);
		}
	}
}
