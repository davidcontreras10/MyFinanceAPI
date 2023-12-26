using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFAuthorizationDataRepository : BaseEFRepository, IAuthorizationDataRepository
	{
		public EFAuthorizationDataRepository(Models.MyFinanceContext context) : base(context)
		{
		}

		public async Task<IEnumerable<UserAssignedAccess>> GetUserAssignedAccessAsync(
			string userId, 
			ApplicationResources applicationResource = ApplicationResources.Unknown, 
			ResourceActionNames actionName = ResourceActionNames.Unknown)
		{
			return await Context.UserAssignedAccess.AsNoTracking()
				.Where(x =>
					x.UserId == new Guid(userId)
					&& (applicationResource == ApplicationResources.Unknown || x.ApplicationResourceId == (int)applicationResource)
					&& (actionName == ResourceActionNames.Unknown || x.ResourceActionId == (int)actionName)
				)
				.Select(x => new UserAssignedAccess
				{
					ApplicationResource = (ApplicationResources)x.ApplicationResourceId,
					ResourceAccesLevel = (ResourceAccesLevels)x.ResourceAccessLevelId,
					ResourceActionName = (ResourceActionNames)x.ResourceActionId
				})
				.ToListAsync();
		}
	}
}
