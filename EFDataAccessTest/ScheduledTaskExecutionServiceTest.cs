using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class ScheduledTaskExecutionServiceTest
    {
        private static readonly Guid TaskId = Guid.NewGuid();
        private static readonly Guid Owner = Guid.NewGuid();
        private static readonly Guid Actor = Guid.NewGuid();
        private static readonly DateTime PostingDate = new(2020, 1, 14);

        private static TransferScheduledTaskVm Definition() => new()
            { Id = TaskId, AccountId = 1, ToAccountId = 2, SpendTypeId = 3, CurrencyId = 1, Amount = 10 };

        private static ScheduledExecutionWork Work(bool occurrence, BaseScheduledTaskVm definition = null) =>
            new(TaskId, Owner, occurrence ? Actor : Owner, PostingDate,
                JsonConvert.SerializeObject(definition ?? Definition(), new JsonSerializerSettings
                    { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                occurrence ? Guid.NewGuid() : null);

        private static ScheduledExecutionRequest Request(bool occurrence = false) =>
            new(TaskId, occurrence ? Actor : Owner, occurrence ? Guid.NewGuid() : null,
                occurrence ? null : Guid.NewGuid(), occurrence ? null : PostingDate);

        private static ScheduledTaskExecutionScope Scope(bool occurrence = false) =>
            new(occurrence ? Actor : Owner, Owner, TaskId, occurrence);

        [Test]
        public void NewExecutionPathIsDisabledByDefaultBeforeRepositoryAccess()
        {
            var service = new ScheduledTaskExecutionService(null, null, null, new ScheduledExecutionOptions());
            var error = Assert.Throws<ServiceException>(() => service.ExecuteAsync(Request(), Scope()));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task FinancialExecutionUsesSnapshotOwnerActorDateAndOrigin(bool occurrence)
        {
            var called = false;
            var financial = Proxy<IScheduledTasksService>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTasksService.ExecuteDefinitionAsync)));
                Assert.That(((BaseScheduledTaskVm)args[0]).Id, Is.EqualTo(TaskId));
                Assert.That(args[1], Is.EqualTo(PostingDate));
                Assert.That(args[2], Is.EqualTo(occurrence ? ExecuteTaskRequestType.Automatic : ExecuteTaskRequestType.Manual));
                Assert.That(args[3], Is.EqualTo((occurrence ? Actor : Owner).ToString()));
                called = true;
                return Task.FromResult(TaskExecutedResult.Success(TaskId.ToString()));
            });
            var access = Proxy<IAccountAccessRepository>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(Owner));
                var targets = (AccountAccessTargets)args[1];
                Assert.That(targets.AccountIds, Is.EquivalentTo(new[] { 1, 2 }));
                Assert.That(targets.SpendTypeIds, Is.EqualTo(new[] { 3 }));
                return Task.FromResult(true);
            });
            await Service(Work(occurrence), financial, access).ExecuteAsync(Request(occurrence), Scope(occurrence));
            Assert.That(called, Is.True);
        }

        [Test]
        public void OwnerScopeFailurePreventsFinancialEffects()
        {
            var access = Proxy<IAccountAccessRepository>((_, _) => Task.FromResult(false));
            var error = Assert.ThrowsAsync<ServiceException>(() => Service(Work(false), null, access).ExecuteAsync(Request(), Scope()));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void InvalidSnapshotCannotCreateFinancialEffects()
        {
            var invalid = Definition(); invalid.Id = Guid.NewGuid();
            Assert.ThrowsAsync<ServiceException>(() => Service(Work(false, invalid), null, null).ExecuteAsync(Request(), Scope()));
            invalid = Definition(); invalid.ToAccountId = 0;
            Assert.ThrowsAsync<ServiceException>(() => Service(Work(false, invalid), null, null).ExecuteAsync(Request(), Scope()));
        }

        [Test]
        public void ReturnedFinancialFailureThrowsInsteadOfCommittingSuccess()
        {
            var financial = Proxy<IScheduledTasksService>((_, _) => Task.FromResult(TaskExecutedResult.Error("No current period", TaskId.ToString())));
            var access = Proxy<IAccountAccessRepository>((_, _) => Task.FromResult(true));
            var error = Assert.ThrowsAsync<ServiceException>(() => Service(Work(false), financial, access).ExecuteAsync(Request(), Scope()));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public async Task ControllerUsesAuthenticatedIdentityAndRouteTask()
        {
            var input = new ScheduledExecutionInput { ManualRequestId = Guid.NewGuid(), PostingDate = PostingDate };
            var service = Proxy<IScheduledTaskExecutionService>((_, args) =>
            {
                var request = (ScheduledExecutionRequest)args[0];
                Assert.That(request.TaskId, Is.EqualTo(TaskId));
                Assert.That(request.ActorId, Is.EqualTo(Actor));
                Assert.That(request.ManualRequestId, Is.EqualTo(input.ManualRequestId));
                return Task.FromResult(new ScheduledExecutionResult(Guid.NewGuid(), false));
            });
            var scope = new ScheduledTaskExecutionScope(Actor, Actor, TaskId);
            var authorization = Proxy<IScheduledTaskAuthorizationService>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(Actor.ToString())); Assert.That(args[1], Is.EqualTo(TaskId.ToString()));
                Assert.That(args[2], Is.EqualTo(false));
                return Task.FromResult(new ScheduledTaskExecutionAuthorizationResult(scope));
            });
            var controller = new ScheduledExecutionController(service, authorization)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                            { new Claim(ClaimTypes.NameIdentifier, Actor.ToString()) }, "test"))
                    }
                }
            };
            await controller.ExecuteAsync(TaskId, input, CancellationToken.None);
            Assert.That(typeof(ScheduledExecutionController).GetCustomAttributes(typeof(AuthorizeAttribute), true), Has.Length.EqualTo(1));
        }

        [Test]
        public void ControllerRejectsMissingIdentityBeforeService()
        {
            var controller = new ScheduledExecutionController(null, null)
                { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.ExecuteAsync(TaskId, new(), CancellationToken.None));
        }

        private static ScheduledTaskExecutionService Service(ScheduledExecutionWork work,
            IScheduledTasksService financial, IAccountAccessRepository access)
        {
            async Task<ScheduledExecutionResult> CompleteAsync(object[] args)
            {
                await ((Func<ScheduledExecutionWork, CancellationToken, Task>)args[1])(work, CancellationToken.None);
                return new ScheduledExecutionResult(Guid.NewGuid(), false);
            }
            var repository = Proxy<IScheduledExecutionRepository>((_, args) => CompleteAsync(args));
            return new(repository, financial, access, new ScheduledExecutionOptions { Enabled = true });
        }

        private static T Proxy<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, TestProxy>();
            ((TestProxy)(object)proxy).Handler = handler;
            return proxy;
        }

        public class TestProxy : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
