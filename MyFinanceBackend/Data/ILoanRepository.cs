using System;
using System.Collections.Generic;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Data
{
    public interface ILoanRepository : ITransactional
    {
        void AddLoanRecord(string userId, int spendId, string loanName);
        void AddLoanSpend(string userId, int spendId, int loanRecordId);
        void CloseLoan(int loanRecordId);
        AccountPeriod GetAccountPeriodByLoanIdDate(int loanRecordId, DateTime dateTime);
        IEnumerable<LoanReportViewModel> GetLoanDetailRecordsByIds(IEnumerable<int> loanRecordIds);
        IEnumerable<int> GetLoanRecordIdsByCriteria(string userId, int loanRecordStatusId, LoanQueryCriteria criteriaId = LoanQueryCriteria.Invalid,
            IEnumerable<int> accountPeriodIds = null, IEnumerable<int> accountIds = null);
        IEnumerable<ItemModified> DeleteLoan(int loanRecordId, string userId);
    }
}