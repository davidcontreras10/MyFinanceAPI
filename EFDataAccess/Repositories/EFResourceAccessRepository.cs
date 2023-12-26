using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFResourceAccessRepository : BaseEFRepository, IResourceAccessRepository
	{
		public EFResourceAccessRepository(MyFinanceContext context) : base(context)
		{
		}

		public async Task<IEnumerable<ResourceAccessReportRow>> GetResourceAccessReportAsync(
			int? applicationResourceId,
			int? applicationModuleId,
			int? resourceActionId,
			int? resourceAccessLevelId)
		{
			var context = Context;
			var query = from rra in context.ResourceRequiredAccess
						join ra in context.ResourceAction on rra.ResourceActionId equals ra.ResourceActionId
						join ar in context.ApplicationResource on rra.ApplicationResourceId equals ar.ApplicationResourceId
						join ral in context.ResourceAccessLevel on rra.ResourceAccessLevelId equals ral.ResourceAccessLevelId
						join appm in context.ApplicationModule on rra.ApplicationModuleId equals appm.ApplicationModuleId
						where
							(applicationResourceId == null || rra.ApplicationResourceId == applicationResourceId) &&
							(applicationModuleId == null || rra.ApplicationModuleId == applicationModuleId) &&
							(resourceAccessLevelId == null || rra.ResourceAccessLevelId == resourceAccessLevelId) &&
							(resourceActionId == null || rra.ResourceActionId == resourceActionId)
						select new ResourceAccessReportRow
						{
							ApplicationResourceId = ar.ApplicationResourceId,
							ApplicationResourceName = ar.ApplicationResourceName,
							ApplicationModuleId = appm.ApplicationModuleId,
							ApplicationModuleName = appm.ApplicationModuleName,
							ResourceActionId = ra.ResourceActionId,
							ResourceActionName = ra.ResourceActionName,
							ResourceAccessLevelId = ral.ResourceAccessLevelId,
							ResourceAccessLevelName	= ral.ResourceAccessLevelName
						};
			var rows = await query.ToListAsync();
			return rows;
		}
	}
}
