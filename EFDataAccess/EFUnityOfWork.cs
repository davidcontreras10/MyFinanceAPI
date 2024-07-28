using EFDataAccess.Models;
using MyFinanceBackend.Data;
using System.Threading.Tasks;

namespace EFDataAccess
{
	public class EFUnityOfWork(
		MyFinanceContext context, 
		IBankTransactionsRepository bankTransactionsRepository, 
		IFinancialEntitiesRepository financialEntitiesRepository, 
		ICurrenciesRepository currenciesRepository
		) : IUnitOfWork
	{
		public IBankTransactionsRepository BankTransactionsRepository { get; } = bankTransactionsRepository;

		public IFinancialEntitiesRepository FinancialEntitiesRepository { get; } = financialEntitiesRepository;

		public ICurrenciesRepository CurrenciesRepository { get; } = currenciesRepository;

		private readonly MyFinanceContext _context = context;

		public async Task CommitTransactionAsync()
		{
			await _context.Database.CommitTransactionAsync();
		}

		public async Task RollbackAsync()
		{
			await _context.Database.RollbackTransactionAsync();
		}

		public async Task SaveAsync()
		{
			await _context.SaveChangesAsync();
		}

		public async Task StartTransactionAsync()
		{
			await _context.Database.BeginTransactionAsync();
		}
	}
}
