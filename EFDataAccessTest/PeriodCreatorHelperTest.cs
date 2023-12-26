using EFDataAccess;
using EFDataAccess.Models;
using NUnit.Framework;
using System;

namespace EFDataAccessTest
{
	public class PeriodCreatorHelperTest
	{
		[Test]
		public void TestWeeklyAccountGreaterCurrentSuccess()
		{
			var weeklyAccount = GetWeeklyAccount(DayOfWeek.Monday);
			var currentDate = new DateTime(2023, 5, 17);
			var assertInitialDate = new DateTime(2023, 5, 15);
			var assertEndDate = new DateTime(2023, 5, 22);
			var result = PeriodCreatorHelper.GetNewPeriodDates(weeklyAccount, currentDate);
			Assert.AreEqual(assertInitialDate, result.InitialDate);
			Assert.AreEqual(assertEndDate, result.EndDate);
		}

		[Test]
		public void TestWeeklyAccountLowerSuccess()
		{
			var weeklyAccount = GetWeeklyAccount(DayOfWeek.Wednesday);
			var currentDate = new DateTime(2023, 5, 15);
			var assertInitialDate = new DateTime(2023, 5, 10);
			var assertEndDate = new DateTime(2023, 5, 17);
			var result = PeriodCreatorHelper.GetNewPeriodDates(weeklyAccount, currentDate);
			Assert.AreEqual(assertInitialDate, result.InitialDate);
			Assert.AreEqual(assertEndDate, result.EndDate);
		}

		[Test]
		public void TestMonthlyAccountGreaterCurrentSuccess()
		{
			var weeklyAccount = GetMonthlyAccount(1);
			var currentDate = new DateTime(2023, 5, 17);
			var assertInitialDate = new DateTime(2023, 5, 1);
			var assertEndDate = new DateTime(2023, 6, 1);
			var result = PeriodCreatorHelper.GetNewPeriodDates(weeklyAccount, currentDate);
			Assert.AreEqual(assertInitialDate, result.InitialDate);
			Assert.AreEqual(assertEndDate, result.EndDate);
		}

		[Test]
		public void TestMonthlyAccountLowerSuccess()
		{
			var weeklyAccount = GetMonthlyAccount(15);
			var currentDate = new DateTime(2023, 5, 3);
			var assertInitialDate = new DateTime(2023, 4, 15);
			var assertEndDate = new DateTime(2023, 5, 15);
			var result = PeriodCreatorHelper.GetNewPeriodDates(weeklyAccount, currentDate);
			Assert.AreEqual(assertInitialDate, result.InitialDate);
			Assert.AreEqual(assertEndDate, result.EndDate);
		}

		[TestCase("2023-04-28")]
		[TestCase("2023-04-15")]
		[TestCase("2023-05-14")]
		[TestCase("2023-05-03")]
		public void TestMonthlyAccountSuccess(DateTime currentDate)
		{
			var weeklyAccount = GetMonthlyAccount(15);
			var assertInitialDate = new DateTime(2023, 4, 15);
			var assertEndDate = new DateTime(2023, 5, 15);
			var result = PeriodCreatorHelper.GetNewPeriodDates(weeklyAccount, currentDate);
			Assert.AreEqual(assertInitialDate, result.InitialDate);
			Assert.AreEqual(assertEndDate, result.EndDate);

		}

		private static Account GetWeeklyAccount(DayOfWeek cuttOffDay)
		{
			return new Account
			{
				AccountId = 2011,
				PeriodDefinition = new PeriodDefinition
				{
					PeriodDefinitionId = 1,
					CuttingDate = ((int)cuttOffDay + 1).ToString(),
					Repetition = 1,
					PeriodTypeId = 1
				},
			};
		}

		private static Account GetMonthlyAccount(int cutOffday)
		{
			return new Account
			{
				AccountId = 4,
				PeriodDefinition = new PeriodDefinition
				{
					PeriodDefinitionId = 2,
					CuttingDate = cutOffday.ToString(),
					Repetition = 1,
					PeriodTypeId = 2
				},
			};
		}
	}
}
