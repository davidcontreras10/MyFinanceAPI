using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using MyFinanceBackend.Data;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFCurrenciesRepository(MyFinanceContext context) : BaseEFRepository(context), ICurrenciesRepository
	{
		public async Task<IReadOnlyCollection<CurrencyViewModel>> GetCurrenciesByCodesAsync(IEnumerable<string> codes)
		{
			return await Context.Currency.AsNoTracking()
				.Where(x => codes.Contains(x.IsoCode))
				.Select(x => new CurrencyViewModel
				{
					CurrencyId = x.CurrencyId,
					CurrencyName = x.Name,
					Symbol = x.Symbol,
					IsoCode = x.IsoCode
				})
				.ToListAsync();
		}

		public async Task<IReadOnlyCollection<BasicCurrencyViewModel>> GetCurrenciesAsync()
		{
			return await Context.Currency.AsNoTracking()
				.Select(x => new BasicCurrencyViewModel
				{
					CurrencyId = x.CurrencyId,
					CurrencyName = x.Name,
					Symbol = x.Symbol,
					IsoCode = x.IsoCode
				})
				.ToListAsync();
		}
	}
}
