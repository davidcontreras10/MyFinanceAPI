using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceBackend.Services;
using MyFinanceModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountLinkRulesTest
	{
		private const int Crc = 1;
		private const int Usd = 2;
		private const int BacSj = 2;
		private const int Promerica = 3;
		private const int Scotia = 6;
		private const int BankWithoutMethods = 9;

		// Mirrors the real catalog: one default per currency, plus one method per bank in each direction.
		private static readonly IReadOnlyCollection<ConverterMethodInfo> Catalog =
		[
			new(3, "Colones default", Crc, Crc, 1, true),
			new(4, "Dolares default", Usd, Usd, 1, true),
			new(1, "Bac SJ Col-Dol", Crc, Usd, BacSj, false),
			new(1002, "PROME. Col-Dol", Crc, Usd, Promerica, false),
			new(1006, "Scotia Col-Dol", Crc, Usd, Scotia, false),
			new(2, "Bac SJ Dol-Col", Usd, Crc, BacSj, false),
			new(1003, "PROME. Dol-Col", Usd, Crc, Promerica, false),
			new(1007, "Scotia Dol-Col", Usd, Crc, Scotia, false)
		];

		[Test]
		public void SameCurrencyWithEntityUsesDefaultMethod()
		{
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, BacSj, Crc, BacSj, null, Catalog), Is.EqualTo(3));
			Assert.That(AccountLinkRules.ResolveMethodId(Usd, BacSj, Usd, BacSj, null, Catalog), Is.EqualTo(4));
		}

		[Test]
		public void SameCurrencyWithoutEntityUsesDefaultMethodWhateverTheChildEntity()
		{
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, null, Crc, null, null, Catalog), Is.EqualTo(3));
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, Scotia, Crc, null, null, Catalog), Is.EqualTo(3));
		}

		[Test]
		public void DifferentCurrencyWithEntityPicksTheEntityMethodAndIgnoresTheClientValue()
		{
			// Child in colones, main account in dollars at Bac San Jose: source colones -> target dollars.
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, BacSj, Usd, BacSj, null, Catalog), Is.EqualTo(1));
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, BacSj, Usd, BacSj, 1006, Catalog), Is.EqualTo(1));
		}

		[Test]
		public void DifferentCurrencyWithEntityUsesTheRightDirection()
		{
			Assert.That(AccountLinkRules.ResolveMethodId(Usd, Scotia, Crc, Scotia, null, Catalog), Is.EqualTo(1007));
		}

		[Test]
		public void OptionsForEntityMethodAreASingleAutoSelectedMethod()
		{
			var options = AccountLinkRules.GetOptions(Crc, Usd, Promerica, Catalog);
			Assert.That(options.Methods.Select(m => m.Id), Is.EqualTo(new[] { 1002 }));
			Assert.That(options.RequiresChoice, Is.False);
		}

		[Test]
		public void ChildWithADifferentEntityIsRejected()
		{
			var ex = Assert.Throws<ServiceException>(() =>
				AccountLinkRules.ResolveMethodId(Crc, Scotia, Crc, BacSj, null, Catalog));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[Test]
		public void ChildWithoutEntityIsRejectedWhenTheMainAccountHasOne()
		{
			Assert.Throws<ServiceException>(() => AccountLinkRules.ResolveMethodId(Crc, null, Crc, BacSj, null, Catalog));
		}

		[Test]
		public void DifferentCurrencyAndEntityWithoutAMethodIsRejected()
		{
			Assert.Throws<ServiceException>(() =>
				AccountLinkRules.ResolveMethodId(Crc, BankWithoutMethods, Usd, BankWithoutMethods, null, Catalog));
		}

		[Test]
		public void DifferentCurrencyWithoutEntityRequiresTheUserToChoose()
		{
			var options = AccountLinkRules.GetOptions(Crc, Usd, null, Catalog);
			Assert.That(options.Methods.Select(m => m.Id), Is.EquivalentTo(new[] { 1, 1002, 1006 }));
			Assert.That(options.RequiresChoice, Is.True);
		}

		[Test]
		public void DifferentCurrencyWithoutEntityAcceptsAValidChoice()
		{
			Assert.That(AccountLinkRules.ResolveMethodId(Crc, null, Usd, null, 1006, Catalog), Is.EqualTo(1006));
		}

		[Test]
		public void DifferentCurrencyWithoutEntityRejectsAMissingOrInvalidChoice()
		{
			Assert.Throws<ServiceException>(() => AccountLinkRules.ResolveMethodId(Crc, null, Usd, null, null, Catalog));
			// 3 is the colones default: not an exchange method between colones and dollars.
			Assert.Throws<ServiceException>(() => AccountLinkRules.ResolveMethodId(Crc, null, Usd, null, 3, Catalog));
			// 1007 is dollars -> colones: wrong direction.
			Assert.Throws<ServiceException>(() => AccountLinkRules.ResolveMethodId(Crc, null, Usd, null, 1007, Catalog));
		}

		[Test]
		public void DifferentCurrencyWithoutEntityAndNoMethodsIsRejected()
		{
			var onlyDefaults = Catalog.Where(m => m.IsDefault).ToList();
			Assert.Throws<ServiceException>(() => AccountLinkRules.ResolveMethodId(Crc, null, Usd, null, null, onlyDefaults));
		}
	}
}
