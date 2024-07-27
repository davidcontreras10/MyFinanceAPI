using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IUnitOfWork
	{
		IFinancialEntitiesRepository FinancialEntitiesRepository { get; }
		IBankTransactionsRepository BankTransactionsRepository { get; }
		Task StartTransactionAsync();
		Task CommitTransactionAsync();
		Task RollbackAsync();
		Task SaveAsync();
	}
}
