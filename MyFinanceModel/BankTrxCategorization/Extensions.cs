using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceModel.BankTrxCategorization
{
	public static class CacheKeyExtensions
	{
		public static bool EqualsExt(this IGptCacheKey key, IGptCacheKey cacheItem)
		{
			if (key == null || cacheItem == null) return false;
			return key.Description == cacheItem.Description &&
				   key.Amount == cacheItem.Amount &&
				   key.Currency == cacheItem.Currency;
		}
	}
}
