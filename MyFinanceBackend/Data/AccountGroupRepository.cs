using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using DataAccess;
using DContre.MyFinance.StUtilities;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Models;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Data
{
	public class AccountGroupRepository : SqlServerBaseService , IAccountGroupRepository
	{
		#region Constructor

		public AccountGroupRepository(IConnectionConfig config) : base(config)
		{
		}

		#endregion

		#region Methods

		public void DeleteAccountGroup(string userId, int accountGroupId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_ID, accountGroupId)
			};

			ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_GROUP_DELETE, parameters);
		}

		public IEnumerable<AccountGroupDetailResultSet> GetAccountGroupDetails(string userId, IEnumerable<int> accountGroupIds = null)
		{
		    var parmAccountGroupIds = ServicesUtils.CreateIntDataTable(accountGroupIds);
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_IDS, parmAccountGroupIds)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_GROUP_DETAIL, parameters);
			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountGroupDetailResultSet);
			return result;
		}

	    public int AddorEditAccountGroup(AccountGroupClientViewModel accountGroupClientViewModel)
	    {
	        if (accountGroupClientViewModel == null)
	        {
	            throw new ArgumentNullException("accountGroupClientViewModel");
	        }

	        var parameters = new[]
	        {
	            new SqlParameter(DatabaseConstants.PAR_USER_ID, accountGroupClientViewModel.UserId),
	            new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_ID, accountGroupClientViewModel.AccountGroupId),
	            new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_NAME, accountGroupClientViewModel.AccountGroupName),
	            new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_DISPLAY_VALUE,
	                accountGroupClientViewModel.AccountGroupDisplayValue),
	            new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_POSITION,
	                accountGroupClientViewModel.AccountGroupPosition),
	            new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_DISPLAY_DEFAULT,
	                accountGroupClientViewModel.DisplayDefault)
	        };

	        var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_GROUP_ADD_EDIT, parameters);
	        return dataSet != null && dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0
	            ? dataSet.Tables[0].Rows[0].ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID)
	            : 0;
	    }

	    #endregion
	}
}
