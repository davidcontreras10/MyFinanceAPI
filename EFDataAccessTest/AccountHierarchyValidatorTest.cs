using System.Collections.Generic;
using System.Net;
using MyFinanceBackend.Services;
using MyFinanceModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountHierarchyValidatorTest
	{
		private static readonly ISet<int> NoAccounts = new HashSet<int>();

		[Test]
		public void NoParentIsAlwaysValid()
		{
			Assert.DoesNotThrow(() => AccountHierarchyValidator.ValidateParents(5, [], NoAccounts, NoAccounts, 3));
		}

		[Test]
		public void SingleOwnedTopLevelParentIsValid()
		{
			Assert.DoesNotThrow(() => AccountHierarchyValidator.ValidateParents(5, [10], new HashSet<int> { 10 }, NoAccounts, 0));
		}

		[Test]
		public void NewAccountWithSingleParentIsValid()
		{
			Assert.DoesNotThrow(() => AccountHierarchyValidator.ValidateParents(null, [10], new HashSet<int> { 10 }, NoAccounts, 0));
		}

		[Test]
		public void MoreThanOneParentIsRejected()
		{
			var ex = Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [10, 11], new HashSet<int> { 10, 11 }, NoAccounts, 0));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[Test]
		public void SameParentTwiceIsRejected()
		{
			Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [10, 10], new HashSet<int> { 10 }, NoAccounts, 0));
		}

		[Test]
		public void AccountAsItsOwnParentIsRejected()
		{
			Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [5], new HashSet<int> { 5 }, NoAccounts, 0));
		}

		[Test]
		public void ParentNotOwnedByUserIsRejected()
		{
			Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [10], NoAccounts, NoAccounts, 0));
		}

		[Test]
		public void ParentThatIsAlreadyASubAccountIsRejected()
		{
			Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [10], new HashSet<int> { 10 }, new HashSet<int> { 10 }, 0));
		}

		[Test]
		public void AccountWithSubAccountsCannotBecomeASubAccount()
		{
			var ex = Assert.Throws<ServiceException>(() =>
				AccountHierarchyValidator.ValidateParents(5, [10], new HashSet<int> { 10 }, NoAccounts, 2));
			Assert.That(ex.Message, Does.Contain("2 sub-account"));
		}
	}
}
