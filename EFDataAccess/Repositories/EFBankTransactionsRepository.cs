using EFDataAccess.Extensions;
using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFBankTransactionsRepository(MyFinanceContext context)
		: BaseEFRepository(context), IBankTransactionsRepository
	{
		public async Task NewSingleTrxBankTransactionsAsync(IEnumerable<NewSingleTrxBankTransaction> newSingleTrxBankTransactions)
		{
			if (newSingleTrxBankTransactions == null || !newSingleTrxBankTransactions.Any()) return;
			var bankTrxIds = newSingleTrxBankTransactions.Select(x => x.BankTrxId);
			var ids = bankTrxIds.Select(x => x.TransactionId);
			var bankTrxs = await Context.BankTransactions
				.Include(x => x.Transactions)
				.Where(x => ids.Contains(x.BankTransactionId))
				.ToListAsync();
			bankTrxIds = bankTrxIds.Where(x => bankTrxs.Any(b => b.BankTransactionId == x.TransactionId && b.FinancialEntityId == x.FinancialEntityId)).ToList();
			var spendIds = newSingleTrxBankTransactions.Select(x => x.SpendOnPeriodId.SpendId);
			var spendOnPeriods = await Context.SpendOnPeriod
				.Where(x => spendIds.Contains(x.SpendId))
				.ToListAsync();
			foreach (var newSingleTrx in newSingleTrxBankTransactions)
			{
				var dbBankTrx = bankTrxs.FirstOrDefault(x => x.BankTransactionId == newSingleTrx.BankTrxId.TransactionId && x.FinancialEntityId == newSingleTrx.BankTrxId.FinancialEntityId);
				if (dbBankTrx == null)
				{
					throw new InvalidOperationException($"Bank Transaction {newSingleTrx.BankTrxId.TransactionId} not found");
				}

				if (dbBankTrx.Transactions != null && dbBankTrx.Transactions.Count > 0)
				{
					throw new InvalidOperationException($"Bank Transaction {newSingleTrx.BankTrxId.TransactionId} already has transactions");
				}

				var spendOnPeriod = spendOnPeriods.FirstOrDefault(x => x.SpendId == newSingleTrx.SpendOnPeriodId.SpendId && x.AccountPeriodId == newSingleTrx.SpendOnPeriodId.AccountPeriodId);
				if (spendOnPeriod == null)
				{
					throw new InvalidOperationException($"SpendOnPeriod {newSingleTrx.SpendOnPeriodId.SpendId} not found");
				}
				dbBankTrx.Transactions = new List<SpendOnPeriod>
				{
					spendOnPeriod
				};

				dbBankTrx.FileDescription = newSingleTrx.Description;
				dbBankTrx.Status = BankTransactionStatus.Processed;
			}
		}

		public async Task UpdateBankTransactionsDescriptionsAsync(IEnumerable<BankTrxDescription> bankTrxDescriptions)
		{
			if (bankTrxDescriptions == null || !bankTrxDescriptions.Any()) return;
			var ids = bankTrxDescriptions.Select(x => x.BankTrxId.TransactionId);
			var bankTrxs = await Context.BankTransactions
				.Include(x => x.Transactions)
					.ThenInclude(t => t.Spend)
				.Where(x => ids.Contains(x.BankTransactionId))
				.ToListAsync();
			bankTrxs = bankTrxs.Where(x => bankTrxDescriptions.Any(b => b.BankTrxId.TransactionId == x.BankTransactionId && b.BankTrxId.FinancialEntityId == x.FinancialEntityId)).ToList();
			foreach (var bankTrx in bankTrxs.Where(b => b.Transactions != null))
			{
				foreach (var trx in bankTrx.Transactions.Where(sop => sop.Spend != null))
				{
					trx.Spend.Description = bankTrxDescriptions
						.First(b => b.BankTrxId.TransactionId == bankTrx.BankTransactionId && b.BankTrxId.FinancialEntityId == bankTrx.FinancialEntityId).Description;
				}
			}
		}

		public async Task<IReadOnlyCollection<BankTransactionDto>> GetBankTransactionDtoByIdsAsync(IEnumerable<BankTrxId> bankTrxIds)
		{
			if (bankTrxIds == null || !bankTrxIds.Any()) return Array.Empty<BankTransactionDto>();
			var ids = bankTrxIds.Select(x => x.TransactionId);
			var res = await Context.BankTransactions.AsNoTracking()
				.Where(x => ids.Contains(x.BankTransactionId))
				.Include(x => x.Transactions)
					.ThenInclude(x => x.Spend)
				.Include(x => x.Transactions)
					.ThenInclude(x => x.AccountPeriod)
				.Select(x => new BankTransactionDto
					(
						new BankTrxId(x.FinancialEntityId, x.BankTransactionId),
						x.FileDescription,
						x.Status,
						x.Currency.ToCurrencyViewModel(),
						x.OriginalAmount ?? 0,
						x.TransactionDate,
						x.Transactions
						.Select(c => c.ToSpendSpendViewModel())
						.ToList()
					)
				)
				.ToListAsync();

			return res.Where(t => bankTrxIds.Any(bId => bId.FinancialEntityId == t.BankTrxId.FinancialEntityId)).ToList();
		}

		public async Task<IReadOnlyCollection<BasicBankTransactionDto>> GetBasicBankTransactionByIdsAsync(IEnumerable<BankTrxId> bankTrxIds)
		{
			if (bankTrxIds == null || !bankTrxIds.Any()) return Array.Empty<BasicBankTransactionDto>();
			var ids = bankTrxIds.Select(x => x.TransactionId);
			var res = await Context.BankTransactions.AsNoTracking()
				.Where(x => ids.Contains(x.BankTransactionId))
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
			return res.Where(t => bankTrxIds.Any(bId => bId.FinancialEntityId == t.FinancialEntityId)).ToList();
		}

		public async Task<IReadOnlyCollection<BasicBankTransactionDto>> GetBasicBankTransactionByIdsAsync(IEnumerable<string> ids, int financialEntityId)
		{
			if (ids == null || !ids.Any())
			{
				return Array.Empty<BasicBankTransactionDto>();
			}

			return await Context.BankTransactions.AsNoTracking()
				.Where(x => ids.Contains(x.BankTransactionId) && x.FinancialEntityId == financialEntityId)
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

		public async Task<IReadOnlyCollection<SpendOnPeriodId>> ClearTrxsFromBankTrxsAsync(IReadOnlyCollection<BankTrxId> bankTrxIds)
		{
			try
			{
				if (bankTrxIds == null || bankTrxIds.Count == 0) return Array.Empty<SpendOnPeriodId>();
				var trxIds = bankTrxIds.Select(x => x.TransactionId);
				var ids = new List<SpendOnPeriodId>();
				var bankTrxs = await Context.BankTransactions
					.Include(x => x.Transactions)
					.ThenInclude(x => x.Spend)
					.Where(x => trxIds.Contains(x.BankTransactionId))
					.ToListAsync();
				foreach (var bankTrx in bankTrxs
					.Where(trx => bankTrxIds.Any(b => b.TransactionId == trx.BankTransactionId && b.FinancialEntityId == trx.FinancialEntityId))
					.Where(trx => trx.Transactions != null && trx.Transactions.Count > 0))
				{
					ids.AddRange(bankTrx.Transactions.Select(x => new SpendOnPeriodId(x.SpendId, x.AccountPeriodId)));
					bankTrx.Transactions.Clear();
				}

				return ids;
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateBankTrxStatusAsync(IReadOnlyCollection<BankTrxId> bankTrxIds, BankTransactionStatus newStatus)
		{
			if (bankTrxIds == null || bankTrxIds.Count == 0) return;
			var trxIds = bankTrxIds.Select(x => x.TransactionId);
			var bankTrxs = await Context.BankTransactions
				.Where(x => trxIds.Contains(x.BankTransactionId))
				.ToListAsync();
			foreach (var bankTrx in bankTrxs
				.Where(trx => bankTrxIds.Any(b => b.TransactionId == trx.BankTransactionId && b.FinancialEntityId == trx.FinancialEntityId)))
			{
				bankTrx.Status = newStatus;
			}
		}
	}
}
