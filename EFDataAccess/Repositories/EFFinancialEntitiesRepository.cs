using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceWebApiCore.Models;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFFinancialEntitiesRepository(MyFinanceContext context) : BaseEFRepository(context), IFinancialEntitiesRepository
	{
		public async Task<FinancialEntityDto> GetByFinancialEntityFile(FinancialEntityFile financialEntityFile)
		{
			var name = FinancialEntityNames.GetNameByEnum(financialEntityFile);
			return await Context.FinancialEntity.AsNoTracking()
				.Where(e => e.Name == name)
				.Select(e => new FinancialEntityDto
				{
					FinancialEntityId = e.FinancialEntityId,
					FinancialEntityName = e.Name
				})
				.FirstOrDefaultAsync();
		}
	}
}
