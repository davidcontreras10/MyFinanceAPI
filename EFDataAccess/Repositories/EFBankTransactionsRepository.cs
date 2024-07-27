using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFBankTransactionsRepository(MyFinanceContext context) 
		: BaseEFRepository(context), IBankTransactionsRepository
	{
		public async Task<IReadOnlyCollection<BasicBankTransaction>> GetBasicBankTransactionByIdsAsync(IEnumerable<string> ids, int financialEntityId)
		{
			if (ids == null || !ids.Any())
			{
				return Array.Empty<BasicBankTransaction>();
			}

			return await Context.BankTransactions.AsNoTracking()
				.Where(x => ids.Contains(x.BankTransactionId) && x.FinancialEntityId == financialEntityId)
				.Select(x => new BasicBankTransaction
				{
					BankTransactionId = x.BankTransactionId,
					CurrencyId = x.CurrencyId,
					FinancialEntityId = x.FinancialEntityId,
					OriginalAmount = x.OriginalAmount,
					Status = x.Status,
					TransactionDate = x.TransactionDate,
				})
				.ToListAsync();
		}
	}
}
