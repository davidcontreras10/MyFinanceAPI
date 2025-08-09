using EFDataAccess.Extensions;
using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel.BankTransactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFBankTransactionsRepository(MyFinanceContext context)
		: BaseEFRepository(context), IBankTransactionsRepository
	{
		public async Task<IReadOnlyCollection<ToClassifyBankTrx>> GetToClassifyBankTransactionsAsync(
			int financialEntityId, 
			IReadOnlyCollection<string> bankTransactions)
		{
			if (bankTransactions == null || bankTransactions.Count == 0)
			{
				return [];
			}
			var toClassifyTransactions = await Context.BankTransactions.AsNoTracking()
				.Where(x =>
					bankTransactions.Contains(x.BankTransactionId)
					&& x.FinancialEntityId == financialEntityId
					&& x.Status == BankTransactionStatus.Inserted
				)
				.Include(x => x.Currency)
				.Select(x =>
					new ToClassifyBankTrx(
						new BankTrxId(x.FinancialEntityId, x.BankTransactionId),
						x.FileDescription,
						x.OriginalAmount ?? 0,
						x.Currency.IsoCode
					)
				).ToListAsync();

			return toClassifyTransactions;
		}

		public async Task<IReadOnlyCollection<ClassifiedBankTrx>> GetClassifiedBankTransactionsAsync(int financialEntityId, string userId, DateTime? initialDate)
		{
			var userGuid = Guid.Parse(userId);
			var classifiedBankTrxs = await Context.BankTransactions.AsNoTracking()
				.Where(x => x.FinancialEntityId == financialEntityId
					&& (initialDate == null || x.TransactionDate >= initialDate)
					&& x.OriginalAmount > 0
					&& x.Transactions.Count == 1
					&& x.Status == BankTransactionStatus.Processed
					&& x.Transactions.All(t =>
						t.Spend != null &&
						t.Spend.SpendType != null &&
						t.AccountPeriod != null &&
						t.AccountPeriod.Account != null &&
						t.AccountPeriod.Account.UserId == userGuid))
				.Select(x => new ClassifiedBankTrx(
					x.OriginalAmount.Value,
					x.Currency.IsoCode,
					x.FileDescription,
					x.Transactions
						.Select(t => t.AccountPeriod.Account.Name)
						.FirstOrDefault(),
					x.Transactions
						.Select(t => t.Spend.SpendType.Name)
						.FirstOrDefault()
				))
				.ToListAsync();

			return classifiedBankTrxs;
		}

		public async Task UpdateBankTransactionsDatesAsync(IEnumerable<BankTrxDate> trxDates)
		{
			if (trxDates == null || !trxDates.Any()) return;
			var ids = trxDates.Select(x => x.BankTrxId.TransactionId);
			var bankTrxs = await Context.BankTransactions
				.Where(x => ids.Contains(x.BankTransactionId))
				.ToListAsync();
			bankTrxs = bankTrxs.Where(x => trxDates.Any(b => b.BankTrxId.TransactionId == x.BankTransactionId && b.BankTrxId.FinancialEntityId == x.FinancialEntityId)).ToList();
			foreach (var bankTrx in bankTrxs)
			{
				var trxDate = trxDates.FirstOrDefault(x => x.BankTrxId.TransactionId == bankTrx.BankTransactionId && x.BankTrxId.FinancialEntityId == bankTrx.FinancialEntityId).Date;
				bankTrx.TransactionDate = trxDate;
			}

			await Context.SaveChangesAsync();
		}

		public async Task<IReadOnlyCollection<BankTrxAppTrxId>> GetBankTransactionsBySearchCriteriaAsync(IUserSearchCriteria userSearchCriteria)
		{
			if (userSearchCriteria == null)
			{
				return [];
			}

			if (userSearchCriteria is BankTrxSearchCriteria.AppTransactionIds appTransactionIds)
			{
				return await GetBankTransactionsByAppIdsAsync(appTransactionIds.TransactionIds);
			}

			if (userSearchCriteria is BankTrxSearchCriteria.RefNumberSearchCriteria refNumberSearchCriteria)
			{
				return await GetBankTransactionsByRefNumberAsync(refNumberSearchCriteria.RefNumber);
			}

			if (userSearchCriteria is BankTrxSearchCriteria.DateSearchCriteria dateSearchCriteria)
			{
				return await GetBankTransactionsByDateAsync(dateSearchCriteria.Date);
			}

			if (userSearchCriteria is BankTrxSearchCriteria.DescriptionSearchCriteria descriptionSearchCriteria)
			{
				return await GetBankTransactionsByDescriptionAsync(descriptionSearchCriteria.Description);
			}
			throw new InvalidOperationException("Invalid search criteria");
		}

		private async Task<IReadOnlyCollection<BankTrxAppTrxId>> GetBankTransactionsByDescriptionAsync(string description)
		{
			var bankTrxs = await Context.BankTransactions.AsNoTracking()
				.Where(x => x.FileDescription.Contains(description))
				.Include(x => x.Transactions)
				.Select(x => new BankTrxAppTrxId
					(
						new BankTrxId(x.FinancialEntityId, x.BankTransactionId),
						x.Transactions.Select(t => t.SpendId).FirstOrDefault()
					))
				.ToListAsync();

			return bankTrxs;
		}

		private async Task<IReadOnlyCollection<BankTrxAppTrxId>> GetBankTransactionsByDateAsync(DateOnly dateOnly)
		{
			var dateTime = dateOnly.ToDateTime(TimeOnly.MinValue).Date;
			var bankTrxs = await Context.BankTransactions.AsNoTracking()
				.Where(x => x.TransactionDate != null && x.TransactionDate.HasValue && x.TransactionDate.Value.Date == dateTime)
				.Include(x => x.Transactions)
				.Select(x => new BankTrxAppTrxId
					(
						new BankTrxId(x.FinancialEntityId, x.BankTransactionId),
						x.Transactions.Select(t => t.SpendId).FirstOrDefault()
					))
				.ToListAsync();

			return bankTrxs;
		}

		private async Task<IReadOnlyCollection<BankTrxAppTrxId>> GetBankTransactionsByRefNumberAsync(string refNumber)
		{
			if (string.IsNullOrWhiteSpace(refNumber))
			{
				return [];
			}

			var bankTrxs = await Context.BankTransactions.AsNoTracking()
				.Where(x => x.BankTransactionId == refNumber)
				.Include(x => x.Transactions)
				.Select(x => new BankTrxAppTrxId
					(
						new BankTrxId(x.FinancialEntityId, x.BankTransactionId),
						x.Transactions.Select(t => t.SpendId).FirstOrDefault()
					))
				.ToListAsync();

			return bankTrxs;
		}

		private async Task<IReadOnlyCollection<BankTrxAppTrxId>> GetBankTransactionsByAppIdsAsync(IEnumerable<int> appIds)
		{
			if (appIds == null || !appIds.Any())
			{
				return [];
			}

			var bankTrxs = await Context.SpendOnPeriod.AsNoTracking()
				.Where(x => appIds.Contains(x.SpendId) && x.BankTransaction != null)
				.Include(x => x.BankTransaction)
				.Select(x => new BankTrxAppTrxId
					(
						new BankTrxId(x.BankTransaction.FinancialEntityId, x.BankTransaction.BankTransactionId),
						x.SpendId
					))
				.ToListAsync();

			return bankTrxs;
		}

		public async Task NewSingleTrxBankTransactionsAsync(IEnumerable<NewTrxBankTransaction> newTrxBankTransactions)
		{
			if (newTrxBankTransactions == null || !newTrxBankTransactions.Any()) return;
			var bankTrxIds = newTrxBankTransactions.Select(x => x.BankTrxId);
			var ids = bankTrxIds.Select(x => x.TransactionId);
			var bankTrxs = await Context.BankTransactions
				.Include(x => x.Transactions)
				.Where(x => ids.Contains(x.BankTransactionId))
				.ToListAsync();
			bankTrxIds = bankTrxIds.Where(x => bankTrxs.Any(b => b.BankTransactionId == x.TransactionId && b.FinancialEntityId == x.FinancialEntityId)).ToList();
			var spendIds = newTrxBankTransactions.SelectMany(x => x.SpendOnPeriodIds.Select(y => y.SpendId)).Distinct();
			var spendOnPeriods = await Context.SpendOnPeriod
				.Where(x => spendIds.Contains(x.SpendId))
				.ToListAsync();
			foreach (var newSingleTrx in newTrxBankTransactions)
			{
				var dbBankTrx = bankTrxs.FirstOrDefault(x => x.BankTransactionId == newSingleTrx.BankTrxId.TransactionId && x.FinancialEntityId == newSingleTrx.BankTrxId.FinancialEntityId)
					?? throw new InvalidOperationException($"Bank Transaction {newSingleTrx.BankTrxId.TransactionId} not found");
				if (dbBankTrx.Transactions != null && dbBankTrx.Transactions.Count > 0)
				{
					throw new InvalidOperationException($"Bank Transaction {newSingleTrx.BankTrxId.TransactionId} already has transactions");
				}

				var trxSpendOnPeriods = GetSpendOnPeriods(newSingleTrx.SpendOnPeriodIds, spendOnPeriods);
				if (!trxSpendOnPeriods.Any())
				{
					throw new InvalidOperationException($"Bank Transaction {newSingleTrx.BankTrxId.TransactionId} has no spend on periods");
				}

				dbBankTrx.Transactions = trxSpendOnPeriods.ToList();
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
				bankTrx.FileDescription = bankTrxDescriptions
					.First(b => b.BankTrxId == bankTrx.GetId()).Description;
				foreach (var trx in bankTrx.Transactions.Where(sop => sop.Spend != null))
				{
					trx.Spend.Description = bankTrx.FileDescription;
				}
			}
		}

		public async Task<IReadOnlyCollection<BankTransactionDto>> GetBankTransactionDtoByIdsAsync(IEnumerable<BankTrxId> bankTrxIds)
		{
			if (bankTrxIds == null || !bankTrxIds.Any()) return [];
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
					OriginalAmount = x.OriginalAmount,
					Description = x.FileDescription
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
					OriginalAmount = x.OriginalAmount,
					Description = x.FileDescription
				})
				.ToListAsync();
		}

		public async Task AddBasicBankTransactionAsync(IEnumerable<BasicBankTransactionDto> basicBankTransactions)
		{
			var entities = basicBankTransactions.Select(x => new EFBankTransaction
			{
				BankTransactionId = !string.IsNullOrWhiteSpace(x.BankTransactionId) ? x.BankTransactionId : throw new Exception("Bank trx id cannot be empty"),
				CurrencyId = x.CurrencyId,
				FinancialEntityId = x.FinancialEntityId > 0 ? x.FinancialEntityId : throw new Exception("Bank trx id cannot be empty"),
				OriginalAmount = x.OriginalAmount,
				Status = x.Status,
				TransactionDate = x.TransactionDate,
				FileDescription = x.Description
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

		public async Task DeleteAsync(BankTrxId bankTrxId)
		{
			if (bankTrxId == null) return;
			await Context.BankTransactions.RemoveWhereAsync(x => x.BankTransactionId == bankTrxId.TransactionId && x.FinancialEntityId == bankTrxId.FinancialEntityId);
		}

		private static IEnumerable<SpendOnPeriod> GetSpendOnPeriods(IEnumerable<SpendOnPeriodId> spendOnPeriodIds, IEnumerable<SpendOnPeriod> spendOnPeriods)
		{
			return spendOnPeriods.Where(x => spendOnPeriodIds.Any(y => y.SpendId == x.SpendId && y.AccountPeriodId == x.AccountPeriodId));
		}
	}
}
