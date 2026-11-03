using System;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using EFDataAccess.Repositories;
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
    public class ScheduledTaskEditingTest
    {
        [Test]
        public async Task HistoryMaintenanceContinuesWhileExecutionIsPaused()
        {
            var owner = Guid.NewGuid(); var task = Guid.NewGuid(); var edited = false; var revised = false;
            var scope = new ScheduledTaskEditScope(owner, task);
            var model = new ClientEditScheduledTask { TaskId = task.ToString() };
            var automatic = Proxy<IAutomaticTaskRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IAutomaticTaskRepository.EditScheduledTaskAsync)));
                Assert.That(args[0], Is.SameAs(model));
                Assert.That(args[1], Is.SameAs(scope)); edited = true;
                return Task.CompletedTask;
            });
            async Task Revise(object[] args)
            {
                Assert.That(args[0], Is.EqualTo(task)); Assert.That(args[1], Is.EqualTo(owner));
                Assert.That(edited, Is.False);
                await ((Func<Task>)args[2])();
                revised = true;
            }
            var revisions = Proxy<IScheduledScheduleRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledScheduleRepository.ReviseAsync)));
                return Revise(args);
            });
            var service = new ScheduledTasksService(automatic, null, null, null, null,
                revisions, new ScheduledExecutionOptions { Enabled = false, HistoryEnabled = true });
            await service.EditScheduledTaskAsync(model, scope);
            Assert.That(edited && revised, Is.True);
        }

        [Test]
        public async Task ExecutionSwitchAloneDoesNotActivateHistoryTables()
        {
            var edited = false;
            var scope = new ScheduledTaskEditScope(Guid.NewGuid(), Guid.NewGuid());
            var automatic = Proxy<IAutomaticTaskRepository>((_, args) =>
            {
                Assert.That(args[1], Is.SameAs(scope)); edited = true; return Task.CompletedTask;
            });
            var service = new ScheduledTasksService(automatic, null, null, null, null,
                null, new ScheduledExecutionOptions { Enabled = true });
            await service.EditScheduledTaskAsync(new ClientEditScheduledTask { TaskId = Guid.NewGuid().ToString() }, scope);
            Assert.That(edited, Is.True);
        }

        [Test]
        public void DisabledHistoryFailsBeforeOpeningTheDatabaseOrReachingAnEdit()
        {
            var repository = new EFScheduledScheduleRepository(null, TimeProvider.System, null,
                new ScheduledExecutionOptions { Enabled = true });
            var task = Guid.NewGuid(); var actor = Guid.NewGuid();
            var operations = new Func<Task>[]
            {
                () => repository.ActivateAsync(task, actor, DateTimeOffset.UtcNow, "UTC"),
                () => repository.DiscoverAsync(task, actor),
                () => repository.ReviseAsync(task, actor, () => throw new AssertionException("Disabled edit reached")),
                () => repository.GetPendingAsync(actor, 10),
                () => repository.GetSchedulesToScanAsync(actor, 10)
            };
            foreach (var operation in operations)
                Assert.That(Assert.ThrowsAsync<ServiceException>(() => operation()).StatusCode,
                    Is.EqualTo(System.Net.HttpStatusCode.ServiceUnavailable));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EditWithoutApprovedScopeFailsBeforeWriting(bool historyEnabled)
        {
            var service = new ScheduledTasksService(null, null, null, null, null,
                null, new ScheduledExecutionOptions { HistoryEnabled = historyEnabled });
            Assert.ThrowsAsync<ArgumentNullException>(() => service.EditScheduledTaskAsync(
                new ClientEditScheduledTask { TaskId = Guid.NewGuid().ToString() }, null));
        }

        [Test]
        public async Task EditControllerOverridesBodyTaskIdAndForwardsApprovedScope()
        {
            var task = Guid.NewGuid(); var owner = Guid.NewGuid(); var called = false;
            var scope = new ScheduledTaskEditScope(Guid.NewGuid(), task);
            var approved = false;
            var authorization = Proxy<IScheduledTaskAuthorizationService>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAuthorizationService.AuthorizeEditAsync)));
                Assert.That(args[0], Is.EqualTo(owner.ToString()));
                Assert.That(args[1], Is.EqualTo(task.ToString()));
                Assert.That(args[2], Is.EqualTo(42));
                approved = true;
                return Task.FromResult(new ScheduledTaskAuthorizationResult(scope));
            });
            var service = Proxy<IScheduledTasksService>((method, args) =>
            {
                Assert.That(approved, Is.True);
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTasksService.EditScheduledTaskAsync)));
                Assert.That(((ClientEditScheduledTask)args[0]).TaskId, Is.EqualTo(task.ToString()));
                Assert.That(args[1], Is.SameAs(scope));
                called = true; return Task.CompletedTask;
            });
            var controller = new ScheduledTasksController(service, authorization)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, owner.ToString()) }, "test"))
                } }
            };
            await controller.EditScheduledTaskAsync(task.ToString(), new ClientEditScheduledTask
            {
                TaskId = Guid.NewGuid().ToString(), SpendTypeId = 42,
                ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.SpendTypeId }
            });
            Assert.That(called, Is.True);
        }

        private static T Proxy<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, EditingProxy>();
            ((EditingProxy)(object)proxy).Handler = handler;
            return proxy;
        }

        public class EditingProxy : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
