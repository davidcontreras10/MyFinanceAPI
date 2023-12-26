using System;
using Microsoft.Data.SqlClient;
using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceModel;


namespace MyFinanceBackend.Services
{
    public class AccountsPeriodsService : SqlServerBaseService, IAccountsPeriodsService
    {

        public AccountsPeriodsService(IConnectionConfig connectionConfig)
            : base(connectionConfig)
        {
        }

        #region Public Methods

        public void CreateAccountPeriod(string userId, int accountId, DateTime initial, DateTime end, float budget)
        {
            var userParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
            var accountIdParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId);
            var initalDateParameter = new SqlParameter(DatabaseConstants.PAR_START_DATE, initial);
            var endDateParameter = new SqlParameter(DatabaseConstants.PAR_END_DATE, end);
            var budgetParameter = new SqlParameter(DatabaseConstants.PAR_BUDGET, budget);
            ExecuteStoredProcedure(DatabaseConstants.SP_PERIOD_CREATE, userParameter, accountIdParameter,
                                   initalDateParameter, endDateParameter, budgetParameter);
        }

        public AddPeriodData GetAddPeriodData(int accountId, string userId)
        {
            if (accountId == 0)
                return new AddPeriodData();
            var userParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
            var accountIdParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId);
            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_PERIOD_NEXT_VALUES, userParameter,
                                                     accountIdParameter);
            return ServicesUtils.CreateAddPeriodData(dataSet, accountId);
        }

        #endregion
    }
}