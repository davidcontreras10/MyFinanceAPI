using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EFDataAccess.Repositories;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Controllers;
using NUnit.Framework;
using static EFDataAccessTest.ScheduledPreferencesTest;

namespace EFDataAccessTest
{
    public class ScheduledTaskWorkflowAuthorizationTest
    {
        [TestCase(ResourceActionNames.View, true)]
        [TestCase(ResourceActionNames.View, false)]
        [TestCase(ResourceActionNames.Delete, true)]
        [TestCase(ResourceActionNames.Delete, false)]
        public async Task AccessRequiresPersistedOwnershipForExactAction(ResourceActionNames action, bool owned)
        {
            var owner = Guid.NewGuid(); var task = Guid.NewGuid();
            var repository = Proxy<IScheduledTaskAccessRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAccessRepository.IsOwnedByAsync)));
                Assert.That(args[0], Is.EqualTo(task)); Assert.That(args[1], Is.EqualTo(owner));
                return Task.FromResult(owned);
            });
            var result = await new ScheduledTaskAuthorizationService(repository, null).AuthorizeAccessAsync(owner.ToString(), task.ToString(), action);
            Assert.That(result.IsAuthorized, Is.EqualTo(owned));
            if (owned) Assert.That(result.Scope.Action, Is.EqualTo(action)); else Assert.That(result.Scope, Is.Null);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CrossUserExecutionRequiresScheduledWorkAndPersistedAdministrator(bool scheduled, bool administrator)
        {
            var actor = Guid.NewGuid(); var owner = Guid.NewGuid(); var task = Guid.NewGuid();
            var repository = Proxy<IScheduledTaskAccessRepository>((method, args) =>
            {
                if (method.Name == nameof(IScheduledTaskAccessRepository.GetOwnerIdAsync))
                    return Task.FromResult<Guid?>(owner);
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAccessRepository.IsAdministratorAsync)));
                Assert.That(args[0], Is.EqualTo(actor)); return Task.FromResult(administrator);
            });
            var result = await new ScheduledTaskAuthorizationService(repository, null).AuthorizeExecutionAsync(actor.ToString(), task.ToString(), scheduled);
            Assert.That(result.IsAuthorized, Is.EqualTo(scheduled && administrator));
            if (result.IsAuthorized)
            {
                Assert.That(result.Scope.OwnerId, Is.EqualTo(owner)); Assert.That(result.Scope.ActorId, Is.EqualTo(actor));
                Assert.That(result.Scope.AdministratorDispatch, Is.True);
            }
            else Assert.That(result.Scope, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task OwnerMayExecuteEitherIntentWithoutAdministratorGrant(bool scheduled)
        {
            var owner = Guid.NewGuid();
            var repository = Proxy<IScheduledTaskAccessRepository>((method, _) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAccessRepository.GetOwnerIdAsync)));
                return Task.FromResult<Guid?>(owner);
            });
            var result = await new ScheduledTaskAuthorizationService(repository, null).AuthorizeExecutionAsync(owner.ToString(), Guid.NewGuid().ToString(), scheduled);
            Assert.That(result.IsAuthorized, Is.True); Assert.That(result.Scope.AdministratorDispatch, Is.False);
        }

        [TestCase(null)]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidIdentityAndTargetsFailBeforeRepository(string value)
        {
            var service = new ScheduledTaskAuthorizationService(null, null); var valid = Guid.NewGuid().ToString();
            Assert.That((await service.AuthorizeAccessAsync(value, valid, ResourceActionNames.View)).Scope, Is.Null);
            Assert.That((await service.AuthorizeAccessAsync(valid, value, ResourceActionNames.Delete)).Scope, Is.Null);
            Assert.That((await service.AuthorizeExecutionAsync(value, valid, true)).Scope, Is.Null);
            Assert.That((await service.AuthorizeExecutionAsync(valid, value, false)).Scope, Is.Null);
        }

        [TestCase(0, false)]
        [TestCase(0, true)]
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(2, false)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        [TestCase(3, true)]
        public async Task WorkflowControllersDenyOrForwardApprovedScope(int endpoint, bool allowed)
        {
            var actor = Guid.NewGuid(); var task = Guid.NewGuid(); var called = false;
            var action = endpoint == 2 ? ResourceActionNames.Delete : ResourceActionNames.View;
            var scope = new ScheduledTaskAccessScope(Guid.NewGuid(), task, action);
            var authorization = Proxy<IScheduledTaskAuthorizationService>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAuthorizationService.AuthorizeAccessAsync)));
                Assert.That(args[0], Is.EqualTo(actor.ToString())); Assert.That(args[1], Is.EqualTo(task.ToString()));
                Assert.That(args[2], Is.EqualTo(action));
                return Task.FromResult(new ScheduledTaskAccessAuthorizationResult(allowed ? scope : null));
            });
            var service = Proxy<IScheduledTasksService>((method, args) =>
            {
                Assert.That(allowed, Is.True); Assert.That(args[0], Is.SameAs(scope)); called = true;
                return method.Name switch
                {
                    nameof(IScheduledTasksService.RetireAsync) => Task.CompletedTask,
                    nameof(IScheduledTasksService.GetExecutionContextAsync) => Task.FromResult<ScheduledTaskExecutionContext>(null),
                    nameof(IScheduledTasksService.GetExecutionHistoryAsync) => Task.FromResult(new ScheduledTaskHistoryPage(Array.Empty<ScheduledTaskHistoryEntry>(), 0, 50, false)),
                    nameof(IScheduledTasksService.GetExecutedTasksByTaskIdAsync) => Task.FromResult<System.Collections.Generic.IReadOnlyCollection<ExecutedTaskViewModel>>(Array.Empty<ExecutedTaskViewModel>()),
                    _ => throw new AssertionException(method.Name)
                };
            });
            var controller = new ScheduledTasksController(service, authorization) { ControllerContext = Context(actor.ToString()) };
            async Task Execute()
            {
                switch (endpoint)
                {
                    case 0: await controller.GetExecutionContextAsync(task.ToString()); break;
                    case 1: await controller.GetExecutionHistoryAsync(task.ToString()); break;
                    case 2: await controller.DeleteScheduledTaskAsync(task.ToString()); break;
                    case 3:
                        var history = new ExecutedTasksController(service, authorization) { ControllerContext = Context(actor.ToString()) };
                        await history.GetExecutedTasksByTaskIdAsync(task.ToString()); break;
                }
            }
            if (allowed) await Execute();
            else Assert.That(Assert.ThrowsAsync<ServiceException>(() => Execute()).StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(called, Is.EqualTo(allowed));
        }

        [Test]
        public void InvalidScopesAndWrongOperationCannotReachDatabase()
        {
            var owner = Guid.NewGuid(); var task = Guid.NewGuid();
            Assert.Throws<ArgumentException>(() => new ScheduledTaskAccessScope(owner, task, ResourceActionNames.Edit));
            Assert.Throws<ArgumentException>(() => new ScheduledTaskExecutionScope(Guid.NewGuid(), owner, task));
            var repository = new EFScheduledScheduleRepository(null, TimeProvider.System, null, new ScheduledExecutionOptions());
            Assert.That(Assert.ThrowsAsync<ServiceException>(() => repository.RetireAsync(new(owner, task, ResourceActionNames.View))).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(Assert.ThrowsAsync<ServiceException>(() => repository.GetExecutionContextAsync(new(owner, task, ResourceActionNames.View))).StatusCode,
                Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [Test]
        public void LegacyRouteIsBlockedBeforeFinancialWorkWhenHistoryIsEnabled()
        {
            var owner = Guid.NewGuid();
            var repository = new EFScheduledScheduleRepository(null, TimeProvider.System, null, new ScheduledExecutionOptions { HistoryEnabled = true });
            Assert.That(Assert.Throws<ServiceException>(() => repository.ExecuteLegacyAsync(new(owner, owner, Guid.NewGuid()),
                () => throw new AssertionException("Legacy finance reached"))).StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void V2DenialOrMissingAuthorizationResultCannotReachExecution(bool missing)
        {
            var actor = Guid.NewGuid();
            var authorization = Proxy<IScheduledTaskAuthorizationService>((_, _) =>
                Task.FromResult(missing ? null : new ScheduledTaskExecutionAuthorizationResult(null)));
            var controller = new ScheduledExecutionController(null, authorization) { ControllerContext = Context(actor.ToString()) };
            Assert.That(Assert.ThrowsAsync<ServiceException>(() => controller.ExecuteAsync(Guid.NewGuid(),
                new ScheduledExecutionInput { ManualRequestId = Guid.NewGuid(), PostingDate = DateTime.Today }, CancellationToken.None)).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public void V2ServiceRejectsScopeMismatchAndCrossUserExtraBeforeRepository()
        {
            var owner = Guid.NewGuid(); var actor = Guid.NewGuid(); var task = Guid.NewGuid();
            var service = new ScheduledTaskExecutionService(null, null, null, new ScheduledExecutionOptions { Enabled = true });
            var request = new ScheduledExecutionRequest(task, actor, null, Guid.NewGuid(), DateTime.Today);
            Assert.That(Assert.Throws<ServiceException>(() => service.ExecuteAsync(request, new(actor, owner, task, true))).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(Assert.Throws<ServiceException>(() => service.ExecuteAsync(request, new(actor, actor, Guid.NewGuid()))).StatusCode,
                Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }
}
