using MyFinanceBackend.Services;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IUnitOfWork
	{
		IUserRespository UserRepository { get; }
		ITransferRepository TransferRepository { get; }
		IAppTransferRepository AppTransferRepository { get; }
		ISpendTypeRepository SpendTypeRepository { get; }
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
