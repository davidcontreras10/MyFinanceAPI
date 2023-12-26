using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Services;
using MyFinanceModel;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
    public class AuthorizationDataRepository : SqlServerBaseService, IAuthorizationDataRepository
    {
        #region Constructor

        public AuthorizationDataRepository(IConnectionConfig config) : base(config)
        {
        }

        #endregion

        #region Public Methods

        public Task<IEnumerable<UserAssignedAccess>> GetUserAssignedAccessAsync(string userId,
            ApplicationResources applicationResource = ApplicationResources.Unknown, 
            ResourceActionNames actionName = ResourceActionNames.Unknown)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter(DatabaseConstants.PAR_USER_ID, userId)
            };

            if (applicationResource != ApplicationResources.Unknown)
            {
                parameters.Add(new SqlParameter(DatabaseConstants.PAR_APPLICATION_RESOURCE_ID,
                    (int)applicationResource));
            }

            if (actionName != ResourceActionNames.Unknown)
            {
                parameters.Add(new SqlParameter(DatabaseConstants.PAR_RESOURCE_ACTION_ID, (int)actionName));
            }

            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_USER_ASSIGNED_ACCESS_LIST, parameters);
            var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateUserAssignedAccess);
            return Task.FromResult(result);
        }

        #endregion
    }
}
