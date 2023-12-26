using System.Collections.Generic;
using System.Threading.Tasks;
using MyFinanceModel;

namespace MyFinanceBackend.Data
{
	public interface IResourceAccessRepository
	{
		Task<IEnumerable<ResourceAccessReportRow>> GetResourceAccessReportAsync(int? applicationResourceId, int? applicationModuleId, int? resourceActionId, int? resourceAccessLevelId);
	}
}