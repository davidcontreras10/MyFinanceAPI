using MyFinanceModel.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IFinancialEntitiesRepository
	{
		Task<FinancialEntityDto> GetByMatchedName(string name);
	}
}
