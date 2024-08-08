using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IUnitOfWork
	{
		ISpendsRepository SpendsRepository { get; }
		IResourceAccessRepository ResourceAccessRepository { get; }
		ICurrenciesRepository CurrenciesRepository { get; }
		IFinancialEntitiesRepository FinancialEntitiesRepository { get; }
		IBankTransactionsRepository BankTransactionsRepository { get; }
		IAccountRepository AccountRepository { get; }
		Task StartTransactionAsync();
		Task CommitTransactionAsync();
		Task RollbackAsync();
		Task SaveAsync();
	}
}
