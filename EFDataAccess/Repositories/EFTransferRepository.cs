using EFDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Data;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFTransferRepository : BaseEFRepository, ITransferRepository
	{
		public EFTransferRepository(MyFinanceContext context) : base(context)
		{
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
