using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceWebApiCore.Models;
using System.Collections.Generic;
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

		public async Task<IReadOnlyCollection<FinancialEntityDto>> GetByIdsAsync(IEnumerable<int> financialEntityIds)
		{
			var idsList = financialEntityIds?.Distinct().ToList() ?? [];
			if (idsList.Count == 0)
			{
				return [];
			}

			return await Context.FinancialEntity.AsNoTracking()
				.Where(e => idsList.Contains(e.FinancialEntityId))
				.Select(e => new FinancialEntityDto
				{
					FinancialEntityId = e.FinancialEntityId,
					FinancialEntityName = e.Name
				})
				.ToListAsync();
		}
	}
}
