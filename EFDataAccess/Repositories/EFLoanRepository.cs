using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess.Repositories
{
	public class EFLoanRepository : BaseEFRepository, ILoanRepository
	{
		public EFLoanRepository(Models.MyFinanceContext context) : base(context)
		{
		}

		public void AddLoanRecord(string userId, int spendId, string loanName)
		{
			throw new NotImplementedException();
		}

		public void AddLoanSpend(string userId, int spendId, int loanRecordId)
		{
			throw new NotImplementedException();
		}

		public void BeginTransaction()
		{
			throw new NotImplementedException();
		}

		public void CloseLoan(int loanRecordId)
		{
			throw new NotImplementedException();
		}

		public void Commit()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ItemModified> DeleteLoan(int loanRecordId, string userId)
		{
			throw new NotImplementedException();
		}

		public AccountPeriod GetAccountPeriodByLoanIdDate(int loanRecordId, DateTime dateTime)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<LoanReportViewModel> GetLoanDetailRecordsByIds(IEnumerable<int> loanRecordIds)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<int> GetLoanRecordIdsByCriteria(string userId, int loanRecordStatusId, LoanQueryCriteria criteriaId = LoanQueryCriteria.Invalid, IEnumerable<int> accountPeriodIds = null, IEnumerable<int> accountIds = null)
		{
			throw new NotImplementedException();
		}

		public void RollbackTransaction()
		{
			throw new NotImplementedException();
		}
	}
}
