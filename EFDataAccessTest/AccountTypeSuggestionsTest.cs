using MyFinanceBackend.Services;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountTypeSuggestionsTest
	{
		// Account type codes: Checking = 1, Saving = 2, Bank = 3.
		[Test]
		public void MainAccountSuggestsBank()
		{
			Assert.That(AccountTypeSuggestions.ForMainAccount(new[] { 2, 3 }), Is.EqualTo(3));
		}

		[Test]
		public void SubAccountSuggestsSaving()
		{
			Assert.That(AccountTypeSuggestions.ForSubAccount(new[] { 2, 3 }), Is.EqualTo(2));
		}

		[Test]
		public void AnInactiveTypeIsNotSuggested()
		{
			// Only Saving is active: Bank can't be suggested for a main account.
			Assert.That(AccountTypeSuggestions.ForMainAccount(new[] { 2 }), Is.Null);
			Assert.That(AccountTypeSuggestions.ForSubAccount(new[] { 3 }), Is.Null);
		}

		[Test]
		public void NoActiveTypesSuggestsNothing()
		{
			Assert.That(AccountTypeSuggestions.ForMainAccount(new int[0]), Is.Null);
			Assert.That(AccountTypeSuggestions.ForSubAccount(new int[0]), Is.Null);
		}
	}
}
