using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Services;
using MyFinanceModel;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public class ResourceAccessRepository : SqlServerBaseService, IResourceAccessRepository
	{
		#region Constructor

		public ResourceAccessRepository(IConnectionConfig config) : base(config)
		{
		}

		#endregion

		#region Public Methods

		public Task<IEnumerable<ResourceAccessReportRow>> GetResourceAccessReportAsync(int? applicationResourceId, int? applicationModuleId, 
			int? resourceActionId, int? resourceAccessLevelId)
		{
			var parameters = new List<SqlParameter>();
			if(applicationResourceId != null)
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_APPLICATION_RESOURCE_ID, applicationResourceId));
			}

			if (applicationModuleId != null)
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_APPLICATION_MODULE_ID, applicationModuleId));
			}

			if (resourceActionId != null)
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_RESOURCE_ACTION_ID, resourceActionId));
			}

			if (resourceAccessLevelId != null)
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_RESOURCE_ACCESS_LEVEL_ID, resourceAccessLevelId));
			}

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_RESOURCE_ACCESS_REPORT, parameters);
			if(dataSet == null || dataSet.Tables.Count == 0)
			{
				IEnumerable<ResourceAccessReportRow> emptyResult = Array.Empty<ResourceAccessReportRow>();
				return Task.FromResult(emptyResult);
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateResourceAccessReportRow);
			return Task.FromResult(result);
		}

		#endregion
	}
}
