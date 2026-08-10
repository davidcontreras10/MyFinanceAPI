using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IFinancialEntitiesRepository
	{
		Task<FinancialEntityDto> GetByFinancialEntityFile(FinancialEntityFile financialEntityFile);
		Task<IReadOnlyCollection<FinancialEntityDto>> GetByIdsAsync(IEnumerable<int> financialEntityIds);
	}
}
