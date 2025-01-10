using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IDebtRequestService
	{
		Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId);
	}
}
