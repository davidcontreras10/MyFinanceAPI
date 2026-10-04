using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountAiClassificationHintTest
	{
		private const string UserId = "017844b8-a92a-44b0-9faf-e4e7230959b1";

		[TestCase("AI subscriptions")]
		[TestCase(null)]
		public async Task GetsCurrentHintIncludingClearedAccountsWithoutUpdates(string hint)
		{
			var calls = 0;
			var service = Service((method, args) =>
			{
				Assert.That(method.Name, Is.EqualTo(nameof(IAccountRepository.GetAiClassificationHintAsync)));
				Assert.That(args[0], Is.EqualTo(UserId));
				Assert.That(args[1], Is.EqualTo(4016));
				calls++;
				return Task.FromResult(new AiClassifiableAccount(4016, "Ingresos Ahorros", hint));
			});
			var result = await service.GetAiClassificationHintAsync(UserId, 4016);
			Assert.That(result.AccountId, Is.EqualTo(4016));
			Assert.That(result.AccountName, Is.EqualTo("Ingresos Ahorros"));
			Assert.That(result.AiClassificationHint, Is.EqualTo(hint));
			Assert.That(calls, Is.EqualTo(1));
		}

		[Test]
		public void GetMissingOrOtherUsersAccountReturnsNotFound()
		{
			var service = Service((method, args) =>
			{
				Assert.That(args[0], Is.EqualTo(UserId));
				return Task.FromResult<AiClassifiableAccount>(null);
			});
			var ex = Assert.ThrowsAsync<ServiceException>(() => service.GetAiClassificationHintAsync(UserId, 999));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public void GetRejectsInvalidAccountBeforeRepository(int accountId)
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().GetAiClassificationHintAsync(UserId, accountId));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("invalid-user")]
		public void GetRejectsMissingOrInvalidUserBeforeRepository(string userId)
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().GetAiClassificationHintAsync(userId, 4016));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
		}

		[TestCase(" AI subscriptions ", "AI subscriptions")]
		[TestCase("", null)]
		[TestCase(" \r\n\t", null)]
		[TestCase(null, null)]
		public async Task UpdatesOnlyHintForSpecifiedUserAndNormalizesBlankValues(string hint, string expected)
		{
			var calls = 0;
			var service = Service((method, args) =>
			{
				Assert.That(method.Name, Is.EqualTo(nameof(IAccountRepository.UpdateAiClassificationHintAsync)));
				Assert.That(args[0], Is.EqualTo(UserId));
				Assert.That(args[1], Is.EqualTo(4016));
				Assert.That(args[2], Is.EqualTo(expected));
				calls++;
				return Task.FromResult(new AiClassifiableAccount(4016, "Ingresos Ahorros", (string)args[2]));
			});
			var result = await service.UpdateAiClassificationHintAsync(UserId, 4016, new() { AiClassificationHint = hint });
			Assert.That(result.AiClassificationHint, Is.EqualTo(expected));
			Assert.That(result.AccountId, Is.EqualTo(4016));
			Assert.That(calls, Is.EqualTo(1));
		}

		[Test]
		public void MissingOrOtherUsersAccountReturnsNotFound()
		{
			var service = Service((method, args) =>
			{
				Assert.That(args[0], Is.EqualTo(UserId));
				return Task.FromResult<AiClassifiableAccount>(null);
			});
			var ex = Assert.ThrowsAsync<ServiceException>(() => service.UpdateAiClassificationHintAsync(UserId, 999, new() { AiClassificationHint = "Hint" }));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
		}

		[TestCase(0)]
		[TestCase(-1)]
		public void RejectsInvalidAccountIdBeforeRepository(int accountId)
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().UpdateAiClassificationHintAsync(UserId, accountId, new()));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[Test]
		public void RejectsMissingBodyBeforeRepository()
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().UpdateAiClassificationHintAsync(UserId, 4016, null));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("invalid-user")]
		public void RejectsMissingOrInvalidUserBeforeRepository(string userId)
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().UpdateAiClassificationHintAsync(userId, 4016, new()));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
		}

		[Test]
		public void RejectsOversizedHintBeforeRepository()
		{
			var ex = Assert.ThrowsAsync<ServiceException>(() => NoRepository().UpdateAiClassificationHintAsync(UserId, 4016,
				new() { AiClassificationHint = new string('x', ClientAccountAiClassificationHint.MaxHintLength + 1) }));
			Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
		}

		[Test]
		public async Task AcceptsMaximumHintLength()
		{
			var service = Service((method, args) => Task.FromResult(new AiClassifiableAccount(4016, "Account", (string)args[2])));
			var result = await service.UpdateAiClassificationHintAsync(UserId, 4016,
				new() { AiClassificationHint = new string('x', ClientAccountAiClassificationHint.MaxHintLength) });
			Assert.That(result.AiClassificationHint.Length, Is.EqualTo(ClientAccountAiClassificationHint.MaxHintLength));
		}

		[Test]
		public void RequestRequiresExplicitFieldButAllowsNullToClear()
		{
			Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<ClientAccountAiClassificationHint>("{}"));
			var request = JsonConvert.DeserializeObject<ClientAccountAiClassificationHint>("{\"aiClassificationHint\":null}");
			Assert.That(request.AiClassificationHint, Is.Null);
		}

		private static AccountService NoRepository() => Service((method, args) => throw new AssertionException("Repository must not be called."));
		private static AccountService Service(Func<MethodInfo, object[], object> handler)
		{
			var proxy = DispatchProxy.Create<IAccountRepository, AccountRepositoryStub>();
			((AccountRepositoryStub)(object)proxy).Handler = handler;
			return new AccountService(proxy);
		}

		public class AccountRepositoryStub : DispatchProxy
		{
			public Func<MethodInfo, object[], object> Handler { get; set; }
			protected override object Invoke(MethodInfo targetMethod, object[] args) => Handler(targetMethod, args);
		}
	}
}
