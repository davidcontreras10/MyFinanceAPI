using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using DContre.MyFinance.StUtilities;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public class UserRepository : SqlServerBaseService, IUserRespository
	{
		#region Constructor


		public UserRepository(IConnectionConfig connectionConfig)
			: base(connectionConfig)
		{
		}

		#endregion

	    public Task<IEnumerable<AppUser>> GetOwendUsersByUserIdAsync(string userId)
	    {
	        var sqlParameterUsername = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
	        var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_USERS_OWNED_LIST, sqlParameterUsername);
	        var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateUser);
			return Task.FromResult(result);
	    }

		public async Task<AppUser> GetUserByUserIdAsync(string userId)
		{
			var sqlParameterUsername = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = await ExecuteStoredProcedureAsync(DatabaseConstants.SP_USER_LIST, sqlParameterUsername);
			return dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0
					   ? null
					   : ServicesUtils.CreateUser(dataSet.Tables[0].Rows[0]);
		}

		public Task<AppUser> GetUserByUsernameAsync(string username)
	    {
	        var sqlParameterUsername = new SqlParameter(DatabaseConstants.PAR_USERNAME, username);
	        var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_USER_LIST, sqlParameterUsername);
			return dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0
				? Task.FromResult((AppUser)null)
				: Task.FromResult(ServicesUtils.CreateUser(dataSet.Tables[0].Rows[0]));
        }

		public async Task<LoginResult> AttemptToLoginAsync(string username, string encryptedPassword)
		{
			var sqlParameterUsername = new SqlParameter(DatabaseConstants.PAR_USERNAME, username);
			var sqlParameterPassword = new SqlParameter(DatabaseConstants.PAR_PASSWORD, encryptedPassword);
			var dataSet = await ExecuteStoredProcedureAsync(DatabaseConstants.SP_LOGIN_ATTEMPT, sqlParameterUsername,
													 sqlParameterPassword);
			if (dataSet == null || dataSet.Tables.Count == 0)
				return null;
			var dataRow = dataSet.Tables[0].Rows.Count > 0 ? dataSet.Tables[0].Rows[0] : null;
			var userDataRow = dataSet.Tables.Count > 1 && dataSet.Tables[1].Rows.Count > 0
									  ? dataSet.Tables[1].Rows[0]
									  : null;
			return ServicesUtils.CreateResultLogin(dataRow, userDataRow);
		}

		public async Task<bool> SetPasswordAsync(string userId, string encryptedPassword)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_PASSWORD, encryptedPassword)
			};

			var dataSet = await ExecuteStoredProcedureAsync(DatabaseConstants.SP_USER_SET_PASSWORD, parameters);
			return dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0;
		}

	    public Task<bool> UpdateUserAsync(ClientEditUser user)
	    {
	        var modifiedParameter =
	            new SqlParameter(DatabaseConstants.PAR_OUTPUT_MODIFIED, SqlDbType.Bit)
	            {
	                Direction = ParameterDirection.Output
	            };

	        var parameters = new[]
	        {
	            new SqlParameter(DatabaseConstants.PAR_USER_ID, user.UserId),
	            new SqlParameter(DatabaseConstants.PAR_USERNAME, user.Username),
                new SqlParameter(DatabaseConstants.PAR_NAME,user.Name),
	            new SqlParameter(DatabaseConstants.PAR_PRIMARY_EMAIL,user.Email),
                modifiedParameter
            };

	        ExecuteStoredProcedure(DatabaseConstants.SP_USERS_EDIT, parameters);
	        var result = (bool) modifiedParameter.Value;
			return Task.FromResult(result);
	    }

	    public Task<string> AddUserAsync(ClientAddUser user)
	    {
	        if (user == null)
	        {
	            throw new ArgumentNullException(nameof(user));
	        }

	        var parameters = new[]
	        {
	            new SqlParameter(DatabaseConstants.PAR_USER_ID, user.UserId),
	            new SqlParameter(DatabaseConstants.PAR_USERNAME, user.Username),
	            new SqlParameter(DatabaseConstants.PAR_NAME,user.Name),
	            new SqlParameter(DatabaseConstants.PAR_PRIMARY_EMAIL,user.Email),
	            new SqlParameter(DatabaseConstants.PAR_PASSWORD, user.EncryptedPassword),
	            new SqlParameter(DatabaseConstants.PAR_PASSWORD, user.CreatedByUserId)
            };

	        var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_USERS_ADD, parameters);
	        if (dataSet.Tables.Count < 1 || dataSet.Tables[0].Rows.Count < 1)
	        {
				return Task.FromResult(string.Empty);   
	        }

			return Task.FromResult(dataSet.Tables[0].Rows[0].ToString(DatabaseConstants.COL_USER_ID));
	    }

        void ITransactional.BeginTransaction()
        {
            throw new NotImplementedException();
        }

        void ITransactional.RollbackTransaction()
        {
            throw new NotImplementedException();
        }

        void ITransactional.Commit()
        {
            throw new NotImplementedException();
        }
    }
}
