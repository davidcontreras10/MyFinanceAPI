using EFDataAccess.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess
{
	public static class PeriodCreatorHelper
	{
		public static NewPeriodDatesResult GetNewPeriodDates(Account account, DateTime currentDate)
		{
			if (account?.PeriodDefinition?.PeriodTypeId == null)
			{
				throw new ArgumentNullException(nameof(account));
			}

			if (account.PeriodDefinition.PeriodTypeId == 1)
			{
				return CreateWeeklyPeriod(currentDate, account.PeriodDefinition.CuttingDate, account.PeriodDefinition.Repetition);
			}

			if (account.PeriodDefinition.PeriodTypeId == 2)
			{
				return CreateMonthlyPeriod(currentDate, account.PeriodDefinition.CuttingDate, account.PeriodDefinition.Repetition);
			}
			throw new NotImplementedException();
		}

		private static NewPeriodDatesResult CreateWeeklyPeriod(DateTime currentDate, string cutOffDatePar, int? repetition)
		{
			if (repetition == null || repetition == 0)
			{
				repetition = 1;
			}

			var cutOffDay = int.TryParse(cutOffDatePar, out var res) ? res : 1;
			cutOffDay -= 1; //convert to .NET DOW.
			var currentDayOfWeek = (int)currentDate.DayOfWeek;
			var daysDifference = currentDayOfWeek >= cutOffDay
				? currentDayOfWeek - cutOffDay
				: 7 - (cutOffDay - currentDayOfWeek);
			daysDifference *= -1;
			var initialDate = currentDate.AddDays(daysDifference).Date;
			var endDate = initialDate.AddDays(7 * repetition.Value).Date;
			return new NewPeriodDatesResult(initialDate, endDate);
		}

		private static NewPeriodDatesResult CreateMonthlyPeriod(DateTime currentDate, string cutOffDatePar, int? repetition)
		{
			if (repetition == null || repetition == 0)
			{
				repetition = 1;
			}

			var cutOffDay = int.TryParse(cutOffDatePar, out var res) ? res : 1;
			var dayOfMonth = currentDate.Day;
			var daysDifference = dayOfMonth >= cutOffDay
				? dayOfMonth - cutOffDay
				: dayOfMonth + (DateTime.DaysInMonth(currentDate.Year, currentDate.Month) - cutOffDay) -1;
			daysDifference *= -1;
			var initialDate = currentDate.AddDays(daysDifference).Date;
			var endDate = initialDate.AddMonths(1);
			return new NewPeriodDatesResult(initialDate, endDate);
		}
	}
}
