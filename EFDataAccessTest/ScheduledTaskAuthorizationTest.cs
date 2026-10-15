using System;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceWebApiCore.Controllers;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class ScheduledTaskAuthorizationTest
    {
        private static readonly Guid Owner = Guid.NewGuid();
        private static readonly Guid Target = Guid.NewGuid();

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidIdentityOrTaskDeniesWithoutRepositoryAccess(string value)
        {
            var service = new ScheduledTaskAuthorizationService(null, null);
            Assert.That((await service.AuthorizeEditAsync(value, Target.ToString())).Scope, Is.Null);
            Assert.That((await service.AuthorizeEditAsync(Owner.ToString(), value)).Scope, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public async Task InvalidReplacementCategoryDeniesBeforeRepositoryAccess(int category)
        {
            Assert.That((await new ScheduledTaskAuthorizationService(null, null)
                .AuthorizeEditAsync(Owner.ToString(), Target.ToString(), category)).Scope, Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task OnlyPersistedOwnerReceivesScope(bool owned)
        {
            var tasks = Proxy<IScheduledTaskAccessRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAccessRepository.IsOwnedByAsync)));
                Assert.That(args[0], Is.EqualTo(Target)); Assert.That(args[1], Is.EqualTo(Owner));
                return Task.FromResult(owned);
            });
            var result = await new ScheduledTaskAuthorizationService(tasks, null)
                .AuthorizeEditAsync(Owner.ToString(), Target.ToString());
            Assert.That(result.IsAuthorized, Is.EqualTo(owned));
            if (owned)
            {
                Assert.That(result.Scope.OwnerId, Is.EqualTo(Owner));
                Assert.That(result.Scope.TaskId, Is.EqualTo(Target));
            }
            else Assert.That(result.Scope, Is.Null);
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task ReplacementCategoryMustBeWithinOwnerScope(bool categoryAllowed)
        {
            var tasks = Proxy<IScheduledTaskAccessRepository>((_, _) => Task.FromResult(true));
            var accounts = Proxy<IAccountAccessRepository>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(Owner));
                Assert.That(((AccountAccessTargets)args[1]).SpendTypeIds, Is.EqualTo(new[] { 42 }));
                return Task.FromResult(categoryAllowed);
            });
            var result = await new ScheduledTaskAuthorizationService(tasks, accounts)
                .AuthorizeEditAsync(Owner.ToString(), Target.ToString(), 42);
            Assert.That(result.IsAuthorized, Is.EqualTo(categoryAllowed));
            if (!categoryAllowed) Assert.That(result.Scope, Is.Null);
        }

        [Test]
        public void ScopeCannotHaveEmptyConstraints()
        {
            Assert.Throws<ArgumentException>(() => new ScheduledTaskEditScope(Guid.Empty, Target));
            Assert.Throws<ArgumentException>(() => new ScheduledTaskEditScope(Owner, Guid.Empty));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ControllerDenialOrMissingResultCannotReachOperation(bool missingResult)
        {
            var authorization = Proxy<IScheduledTaskAuthorizationService>((_, args) =>
            {
                Assert.That(args[2], Is.Null); // Unmodified category fields are not requested targets.
                return Task.FromResult(missingResult ? null : new ScheduledTaskAuthorizationResult(null));
            });
            var controller = Controller(authorization);
            var error = Assert.ThrowsAsync<ServiceException>(() => controller.EditScheduledTaskAsync(Target.ToString(),
                new ClientEditScheduledTask { SpendTypeId = 42, ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.Description } }));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ControllerRequiresIdentityBeforeAuthorization(string identity)
        {
            var controller = Controller(null, identity);
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.EditScheduledTaskAsync(Target.ToString(),
                new ClientEditScheduledTask()));
        }

        private static ScheduledTasksController Controller(IScheduledTaskAuthorizationService authorization,
            string identity = "017844b8-a92a-44b0-9faf-e4e7230959b1") =>
            new(null, authorization)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(identity == null ? Array.Empty<Claim>() :
                        new[] { new Claim(ClaimTypes.NameIdentifier, identity) }, "test"))
                } }
            };

        private static T Proxy<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, AuthorizationProxy>();
            ((AuthorizationProxy)(object)proxy).Handler = handler;
            return proxy;
        }

        public class AuthorizationProxy : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
