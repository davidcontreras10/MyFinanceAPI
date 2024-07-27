using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IFinancialEntitiesRepository
	{
		Task<FinancialEntityDto> GetByFinancialEntityFile(FinancialEntityFile financialEntityFile);
	}
}
