using EFDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFTransferRepository(MyFinanceContext context, ILogger<EFTransferRepository> logger) : BaseEFRepository(context), ITransferRepository
	{
		public async Task ExecuteMigrationAsync()
		{
			var transferRecords = await Context.TransferRecord
				.Include(tr => tr.Spend)
				.GroupBy(tr => tr.TransferRecordId)
				.ToListAsync();
			var newTransferRecords = new List<EFAppTransfer>();
			foreach (var oldTransferRecs in transferRecords)
			{
				try
				{
					if (oldTransferRecs.Count() != 2)
					{
						logger.LogError($"Transfer record with id {oldTransferRecs.Key} has {oldTransferRecs.Count()} records");
						continue;
					}

					var spend = oldTransferRecs.FirstOrDefault(tr => tr.Spend != null && tr.Spend.AmountTypeId == (int)TransactionTypeIds.Spend);
					if (spend == null)
					{
						logger.LogError($"Transfer record with id {oldTransferRecs.Key} has no spend record");
					}

					var saving = oldTransferRecs.FirstOrDefault(tr => tr.Spend != null && tr.Spend.AmountTypeId == (int)TransactionTypeIds.Saving);
					if (saving == null)
					{
						logger.LogError($"Transfer record with id {oldTransferRecs.Key} has no saving record");
					}

					EFAppTransfer newTransfer = new()
					{
						SourceAppTrxId = spend.SpendId,
						DestinationAppTrxId = saving.SpendId
					};

					await Context.AppTransfers.AddAsync(newTransfer);
					Context.TransferRecord.RemoveRange(oldTransferRecs);

				}
				catch(Exception ex)
				{
					logger.LogError(ex, $"Error migrating transfer record with id {oldTransferRecs.Key}");
				}
			}

			try
			{
				await Context.SaveChangesAsync();
				logger.LogInformation("Transfer records migrated successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error saving transfer records");
			}
		}

		public async Task AddTransferRecordAsync(IEnumerable<int> spendIds, string userId)
		{
			var currentId = await Context.TransferRecord.AsNoTracking()
				.OrderByDescending(x => x.TransferRecordId)
				.Select(x => x.TransferRecordId)
				.FirstOrDefaultAsync();
			var nextId = currentId + 1;
			var transferItems = spendIds
				.Select(x => new TransferRecord
				{
					TransferRecordId = nextId,
					SpendId = x
				});
			await Context.TransferRecord.AddRangeAsync(transferItems);
			await Context.SaveChangesAsync();
		}

		public void RollbackTransaction()
		{
			Context.Database.RollbackTransaction();
		}

		public void BeginTransaction()
		{
			Context.Database.BeginTransaction();
		}

		public void Commit()
		{
			Context.Database.CommitTransaction();
		}

		public async Task<int> GetDefaultCurrencyConvertionMethodsAsync(int originAccountId, int amountCurrencyId, int destinationCurrencyId, string userId)
		{
			if (amountCurrencyId == destinationCurrencyId)
			{
				var method = await Context.CurrencyConverter.AsNoTracking()
					.Where(x => x.CurrencyIdTwo == destinationCurrencyId && x.CurrencyIdOne == destinationCurrencyId)
					.Include(x => x.CurrencyConverterMethod)
					.FirstOrDefaultAsync();
				return method.CurrencyConverterMethod?.FirstOrDefault().CurrencyConverterMethodId ?? 0;
			}

			var financialEntityId = await Context.Account.AsNoTracking()
				.Where(acc => acc.AccountId == originAccountId)
				.Select(x => x.FinancialEntityId)
				.FirstOrDefaultAsync();

			if (financialEntityId == null || financialEntityId < 1)
			{
				return 0;
			}

			var ccm = await Context.CurrencyConverterMethod.AsNoTracking()
				.Include(x => x.CurrencyConverter)
				.Where(x =>
					x.FinancialEntityId == financialEntityId
					&& x.CurrencyConverter.CurrencyIdOne == amountCurrencyId
					&& x.CurrencyConverter.CurrencyIdTwo == destinationCurrencyId)
				.FirstOrDefaultAsync();
			return ccm?.CurrencyConverterMethodId ?? 0;
		}

		public async Task<IEnumerable<AccountViewModel>> GetPossibleDestinationAccountAsync(int accountPeriodId, int currencyId, string userId)
		{
			try
			{
				var storedProcedureCall = $"EXEC {DatabaseConstants.SP_TRANSFER_POSSIBLE_DESTINATION_ACCOUNTS} @pAccountPeriodId, @pCurrencyId, @pUserId";

				var accountIdNameItems = await Context.Set<EFAccountIdName>().FromSqlRaw(storedProcedureCall,
					parameters: new object[]
				{
					new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_ID, accountPeriodId),
					new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, currencyId),
					new SqlParameter(DatabaseConstants.PAR_USER_ID, userId)
				}).ToListAsync();

				return accountIdNameItems.Select(x => new AccountViewModel
				{
					AccountId = x.AccountId,
					AccountName = x.AccountName
				});
			}
			catch (Exception ex)
			{
				throw;
			}

		}
	}
}
