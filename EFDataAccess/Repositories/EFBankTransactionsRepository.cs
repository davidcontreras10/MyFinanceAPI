using EFDataAccess.Extensions;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFBankTransactionsRepository(MyFinanceContext context)
		: BaseEFRepository(context), IBankTransactionsRepository
	{
		public async Task<IReadOnlyCollection<BasicBankTransactionDto>> GetBasicBankTransactionByIdsAsync(IEnumerable<string> ids, int financialEntityId)
		{
			if (ids == null || !ids.Any())
			{
				return Array.Empty<BasicBankTransactionDto>();
			}

			return await Context.BankTransactions.AsNoTracking()
				.Where(x => ids.Contains(x.BankTransactionId) && x.FinancialEntityId == financialEntityId)
				.Include(x => x.OriginalAccount)
				.Include(x => x.Transactions)
					.ThenInclude(x => x.Spend)
				.Include(x => x.Transactions)
					.ThenInclude(x => x.AccountPeriod)
				.Select(x => new BasicBankTransactionDto
				{
					BankTransactionId = x.BankTransactionId,
					CurrencyId = x.CurrencyId,
					FinancialEntityId = x.FinancialEntityId,
					Status = x.Status,
					TransactionDate = x.TransactionDate,
					Transactions = x.Transactions
						.Select(c => c.ToSpendSpendViewModel())
						.ToList(),
					OriginalAmount = x.OriginalAmount
				})
				.ToListAsync();
		}

		public async Task AddBasicBankTransactionAsync(IEnumerable<BasicBankTransactionDto> basicBankTransactions)
		{
			var entities = basicBankTransactions.Select(x => new EFBankTransaction
			{
				BankTransactionId = x.BankTransactionId,
				CurrencyId = x.CurrencyId,
				FinancialEntityId = x.FinancialEntityId,
				OriginalAmount = x.OriginalAmount,
				Status = x.Status,
				TransactionDate = x.TransactionDate
			});

			await Context.BankTransactions.AddRangeAsync(entities);
		}
	}
}
