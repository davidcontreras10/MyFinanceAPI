using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFFinancialEntitiesRepository : BaseEFRepository, IFinancialEntitiesRepository
	{
		public EFFinancialEntitiesRepository(MyFinanceContext context) : base(context)
		{
		}

		public async Task<FinancialEntityDto> GetByMatchedName(string name)
		{
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
