using EFDataAccess.Models;
using EFDataAccess.Models.Customs;
using Microsoft.EntityFrameworkCore;
using MyFinanceModel.Records;
using System.Linq;

namespace EFDataAccess.Extensions
{
	internal static class EntitiesExtensions
	{
		public static BankTrxId GetId(this EFBankTransaction entity)
		{
			return new BankTrxId(entity.FinancialEntityId, entity.BankTransactionId);
		}

		public static IQueryable<EFDebtRequest> IncludeAllQuery(this IQueryable<EFDebtRequest> query, bool includeAppTrxs)
		{
			query = query
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser);
			if (includeAppTrxs)
			{ 
				query = query
				.Include(x => x.DebtorSpends)
					.ThenInclude(x => x.SpendOnPeriod)
						.ThenInclude(x => x.AccountPeriod)
				.Include(x => x.DebtorSpends)
					.ThenInclude(x => x.AmountCurrency)
				.Include(x => x.CreditorSpends)
					.ThenInclude(x => x.SpendOnPeriod)
						.ThenInclude(x => x.AccountPeriod)
				.Include(x => x.CreditorSpends)
					.ThenInclude(x => x.AmountCurrency);
			}

			return query;
		}

		public static IQueryable<EFDebtRequestAdditional> IncludeAllAdditionalQuery(this IQueryable<EFDebtRequest> query, bool includeAppTrxs)
		{
			query = query
				.Include(x => x.Currency)
				.Include(x => x.CreditorUser)
				.Include(x => x.DebtorUser);
			if (includeAppTrxs)
			{
				query = query
				.Include(x => x.DebtorSpends)
					.ThenInclude(x => x.SpendOnPeriod)
						.ThenInclude(x => x.AccountPeriod)
				.Include(x => x.DebtorSpends)
					.ThenInclude(x => x.AmountCurrency)
				.Include(x => x.CreditorSpends)
					.ThenInclude(x => x.SpendOnPeriod)
						.ThenInclude(x => x.AccountPeriod)
				.Include(x => x.CreditorSpends)
					.ThenInclude(x => x.AmountCurrency);
			}

			var additionalQuery = query
				.Select(x => new EFDebtRequestAdditional
				{
					EFDebtRequest = x,
					DebtorSpendsCount = x.DebtorSpends.Count(),
					CreditorSpendsCount = x.CreditorSpends.Count()
				});

			return additionalQuery;
		}
	}
}
