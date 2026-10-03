using System.Net;
using MyFinanceBackend.Services;
using MyFinanceModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountTypeRulesTest
	{
		[Test]
		public void ActiveTypeCanBeChosen()
		{
			Assert.DoesNotThrow(() => AccountTypeRules.ValidateSelectable(
				new AccountTypeUsage { Exists = true, IsActive = true, AccountHasIt = false }));
		}

		[Test]
		public void InactiveTypeCannotBeChosenForANewAccount()
		{
			var ex = Assert.Throws<ServiceException>(() => AccountTypeRules.ValidateSelectable(
				new AccountTypeUsage { Exists = true, IsActive = false, AccountHasIt = false }));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[Test]
		public void AccountThatAlreadyHasAnInactiveTypeKeepsIt()
		{
			Assert.DoesNotThrow(() => AccountTypeRules.ValidateSelectable(
				new AccountTypeUsage { Exists = true, IsActive = false, AccountHasIt = true }));
		}

		[Test]
		public void UnknownTypeIsRejected()
		{
			Assert.Throws<ServiceException>(() => AccountTypeRules.ValidateSelectable(
				new AccountTypeUsage { Exists = false, IsActive = false, AccountHasIt = false }));
		}
	}
}
