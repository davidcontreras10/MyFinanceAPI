using EFDataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFDataAccess.Helpers
{
	internal static class AccountHelpers
	{
		public static AccountPeriod GetCurrentAccountPeriod(DateTime? dateTime, Account account) => GetCurrentAccountPeriod(dateTime, account.AccountPeriod);

		public static AccountPeriod GetCurrentAccountPeriod(DateTime? dateTime, ICollection<AccountPeriod> accountPeriods)
		{
			if (dateTime == null)
			{
				dateTime = DateTime.UtcNow;
			}

			if (accountPeriods == null || !accountPeriods.Any())
			{
				return null;
			}

			return accountPeriods.FirstOrDefault(accp => InAccountPeriod(accp, dateTime.Value));
		}

		private static bool InAccountPeriod(AccountPeriod accountPeriod, DateTime dateTime)
		{
			return dateTime >= accountPeriod.InitialDate.Value.Date && dateTime < accountPeriod.EndDate.Value.Date;
		}
	}
}
