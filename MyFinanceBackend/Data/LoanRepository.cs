using System;
using DataAccess;
using MyFinanceBackend.Constants;
using Microsoft.Data.SqlClient;
using System.Linq;
using MyFinanceBackend.Services;
using MyFinanceModel;
using System.Collections.Generic;
using MyFinanceModel.ViewModel;
using MyFinanceBackend.Models;
using System.Data;
using DContre.MyFinance.StUtilities;

namespace MyFinanceBackend.Data
{
    public class LoanRepository : SqlServerBaseService, ILoanRepository
    {
        #region Constructor

        public LoanRepository(IConnectionConfig config) : base(config)
        {
        }

        #endregion

        #region Public Methods

        public IEnumerable<LoanReportViewModel> GetLoanDetailRecordsByIds(IEnumerable<int> loanRecordIds)
        {
            var idsParameter = ServicesUtils.CreateIntDataTable(loanRecordIds);

            var parameters = new[]
{
                new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_IDS, idsParameter)
            };

            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_DETAIL_BY_IDS, parameters);
            var resultSets = CreateDetailLoanResultSet(dataSet.Tables[0]);
            var result = CreateLoanReportViewModel(resultSets);
            return result;
        }

        public void AddLoanRecord(string userId, int spendId, string loanName)
        {
            var parameters = new[]
{
                new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
                new SqlParameter(DatabaseConstants.PAR_LOAN_NAME, loanName),
                new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId)
            };

            ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_ADD_EDIT, parameters);
        }

        public void AddLoanSpend(string userId, int spendId, int loanRecordId)
        {
            var parameters = new[]
            {
                new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
                new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_ID, loanRecordId),
                new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId)
            };

            ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_SPEND_ADD, parameters);
        }

        public AccountPeriod GetAccountPeriodByLoanIdDate(int loanRecordId, DateTime dateTime)
        {
            var parameters = new[]
            {
                new SqlParameter(DatabaseConstants.PAR_SPEND_DATE, dateTime),
                new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_ID, loanRecordId)
            };

            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_PERIOD_BY_LOAN_DATE, parameters);
            var accountPeriods = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountPeriod);
            return accountPeriods.FirstOrDefault();
        }

        public IEnumerable<int> GetLoanRecordIdsByCriteria(string userId, int loanRecordStatusId, LoanQueryCriteria criteriaId = LoanQueryCriteria.Invalid,
            IEnumerable<int> accountPeriodIds = null, IEnumerable<int> accountIds = null)
        {
            if ((accountPeriodIds == null || !accountPeriodIds.Any()) && (accountIds == null || !accountIds.Any()) )
            {
                criteriaId = LoanQueryCriteria.UserId;
            }

            if (criteriaId == LoanQueryCriteria.Invalid)
            {
                throw new ArgumentException(@"Invalid criteria id", nameof(criteriaId));
            }

            var parameters = new List<SqlParameter>();
            if (criteriaId == LoanQueryCriteria.AccountId && accountIds != null && accountIds.Any())
            {
                var tableAccountIds = ServicesUtils.CreateIntDataTable(accountIds);
                parameters.Add(new SqlParameter(DatabaseConstants.PAR_ACCOUNT_IDS, tableAccountIds));
            }

            if (criteriaId == LoanQueryCriteria.AccountPeriodId && accountPeriodIds != null && accountPeriodIds.Any())
            {
                var tableAccountPeriodIds = ServicesUtils.CreateIntDataTable(accountPeriodIds);
                parameters.Add(new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_IDS, tableAccountPeriodIds));
            }

            parameters.Add(new SqlParameter(DatabaseConstants.PAR_USER_ID, userId));
            parameters.Add(new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_STATUS_ID, loanRecordStatusId));
            parameters.Add(new SqlParameter(DatabaseConstants.PAR_CRITERIA_ID, criteriaId));

            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_IDS_LIST, parameters);
            if(dataSet == null || dataSet.Tables.Count == 0)
            {
                return new int[0];
            }

            var ids = dataSet.Tables[0].Rows.Cast<DataRow>().
                Select(row => row.ToInt(DatabaseConstants.COL_LOAN_RECORD_ID, DataRowConvert.ParseBehaviorOption.ThrowException));
            return ids;
        }

        public void CloseLoan(int loanRecordId)
        {
            var parameters = new[]
{
                new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_ID, loanRecordId)
            };

            ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_CLOSE, parameters);
        }

        public IEnumerable<ItemModified> DeleteLoan(int loanRecordId, string userId)
        {
            var parameters = new[]
{
                new SqlParameter(DatabaseConstants.PAR_LOAN_RECORD_ID, loanRecordId),
                new SqlParameter(DatabaseConstants.PAR_USER_ID, userId)
            };

            var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_LOAN_DELETE, parameters);
            var result = ServicesUtils.CreateAccountAffected(dataSet);
            return result;
        }

        #endregion

        #region Privates

        private static IEnumerable<DetailLoanResultSet> CreateDetailLoanResultSet(DataTable dataTable)
        {
            if (dataTable == null)
            {
                return new DetailLoanResultSet[0];
            }

            var result = ServicesUtils.CreateGenericList(dataTable, ServicesUtils.CreateDetailLoanResultSet);
            return result;
        }

        private static IEnumerable<LoanReportViewModel> CreateLoanReportViewModel(IEnumerable<DetailLoanResultSet> detailLoanResultSets)
        {
            if(detailLoanResultSets == null || !detailLoanResultSets.Any())
            {
                return new LoanReportViewModel[0];
            }

            var list = new List<LoanReportViewModel>();
            foreach(var item in detailLoanResultSets)
            {
                var loanViewModel = list.FirstOrDefault(i => i.LoanRecordId == item.LoanRecordId);
                if(loanViewModel == null)
                {
                    loanViewModel = CreateLoanReportViewModel(item);
                    list.Add(loanViewModel);
                }

                if (item.Spend != null)
                {
                    ((List<SpendViewModel>)(loanViewModel.SpendViewModels)).Add(item.Spend);
                }
            }

            return list;
        }

        private static LoanReportViewModel CreateLoanReportViewModel(DetailLoanResultSet detailLoanResultSet)
        {
            if(detailLoanResultSet == null)
            {
                throw new ArgumentNullException(nameof(detailLoanResultSet));
            }

            var result = new LoanReportViewModel
            {
                LoanSpendViewModel = detailLoanResultSet.LoanSpendRecord,
                LoanName = detailLoanResultSet.LoanRecordName,
                LoanRecordId = detailLoanResultSet.LoanRecordId,
                PaymentSumary = detailLoanResultSet.PaymentSummary,
                SpendViewModels = new List<SpendViewModel>(),
                AccountId = detailLoanResultSet.AccountId,
                AccountName = detailLoanResultSet.AccountName
            };

            return result;
        }

        #endregion

        #region SQL Overrides

        public new void BeginTransaction()
        {
            base.BeginTransaction();
        }

        public new void RollbackTransaction()
        {
            base.RollbackTransaction();
        }

        public new void Commit()
        {
            base.Commit();
        }

        #endregion 
    }
}