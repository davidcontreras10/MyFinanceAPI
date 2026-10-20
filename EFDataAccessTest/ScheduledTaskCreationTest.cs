using System;
using System.Threading.Tasks;
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
    public class ScheduledTaskCreationTest
    {
        private static ClientScheduledTask Request(bool transfer) => transfer
            ? new ClientScheduledTask.Transfer { AccountId = 1, ToAccountId = 2, SpendTypeId = 3, CurrencyId = 1, Amount = 10, Description = "Transfer", FrequencyType = ScheduledTaskFrequencyType.Weekly, Days = new[] { 0, 6 } }
            : new ClientScheduledTask.Basic { AccountId = 1, SpendTypeId = 3, CurrencyId = 1, Amount = 10, Description = "Basic", FrequencyType = ScheduledTaskFrequencyType.Monthly, Days = new[] { 1, 28 } };

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CreationUsesApprovedScopeAndRegistersOnlyWhenHistoryEnabled(bool transfer, bool history)
        {
            var scope = new ScheduledTaskCreateScope(Guid.NewGuid()); var model = Request(transfer);
            var registering = false; var created = false; var id = Guid.NewGuid();
            var tasks = Proxy<IAutomaticTaskRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IAutomaticTaskRepository.CreateAsync)));
                Assert.That(args[0], Is.SameAs(scope)); Assert.That(args[1], Is.SameAs(model));
                Assert.That(registering, Is.EqualTo(history)); created = true; return Task.FromResult(id);
            });
            async Task<Guid> Register(object[] args)
            {
                Assert.That(args[0], Is.EqualTo(scope.OwnerId)); registering = true;
                return await ((Func<Task<Guid>>)args[1])();
            }
            var schedules = Proxy<IScheduledScheduleRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledScheduleRepository.RegisterAsync)));
                return Register(args);
            });
            var service = new ScheduledTasksService(tasks, null, null, null, null, schedules, new ScheduledExecutionOptions { HistoryEnabled = history });
            if (transfer) await service.CreateTransferTrxAsync(scope, (ClientScheduledTask.Transfer)model);
            else await service.CreateBasicTrxAsync(scope, (ClientScheduledTask.Basic)model);
            Assert.That(created, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Monthly29IsRejectedBeforeEitherCreationPath(bool history)
        {
            var model = (ClientScheduledTask.Basic)Request(false); model.Days = new[] { 29 };
            var service = new ScheduledTasksService(null, null, null, null, null, null, new ScheduledExecutionOptions { HistoryEnabled = history });
            Assert.ThrowsAsync<ServiceException>(() => service.CreateBasicTrxAsync(new ScheduledTaskCreateScope(Guid.NewGuid()), model));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CreationAuthorizationChecksAllOwnedReferences(bool transfer, bool allowed)
        {
            var owner = Guid.NewGuid(); var request = Request(transfer);
            var accounts = Proxy<IAccountAccessRepository>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(owner));
                Assert.That(((AccountAccessTargets)args[1]).AccountIds, Is.EqualTo(transfer ? new[] { 1, 2 } : new[] { 1 }));
                Assert.That(((AccountAccessTargets)args[1]).SpendTypeIds, Is.EqualTo(new[] { 3 }));
                return Task.FromResult(allowed);
            });
            var result = await new ScheduledTaskAuthorizationService(null, accounts).AuthorizeCreateAsync(owner.ToString(), request);
            Assert.That(result.IsAuthorized, Is.EqualTo(allowed));
            if (allowed) Assert.That(result.Scope.OwnerId, Is.EqualTo(owner)); else Assert.That(result.Scope, Is.Null);
        }

        [TestCase(null)]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidCreationIdentityFailsClosed(string identity) =>
            Assert.That((await new ScheduledTaskAuthorizationService(null, null).AuthorizeCreateAsync(identity, Request(false))).Scope, Is.Null);

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CreationControllerDeniesOrForwardsApprovedScope(bool transfer, bool allowed)
        {
            var actor = Guid.NewGuid().ToString(); var scope = new ScheduledTaskCreateScope(Guid.NewGuid()); var request = Request(transfer); var called = false;
            var authorization = Proxy<IScheduledTaskAuthorizationService>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledTaskAuthorizationService.AuthorizeCreateAsync)));
                Assert.That(args[0], Is.EqualTo(actor)); Assert.That(args[1], Is.SameAs(request));
                return Task.FromResult(new ScheduledTaskCreationAuthorizationResult(allowed ? scope : null));
            });
            var service = Proxy<IScheduledTasksService>((method, args) =>
            {
                Assert.That(allowed, Is.True); Assert.That(args[0], Is.SameAs(scope)); Assert.That(args[1], Is.SameAs(request));
                called = true; return Task.CompletedTask;
            });
            var controller = new ScheduledTasksController(service, authorization) { ControllerContext = Context(actor) };
            async Task Execute()
            {
                if (transfer) await controller.CreateTransferAsync((ClientScheduledTask.Transfer)request);
                else await controller.CreateBasicAsync((ClientScheduledTask.Basic)request);
            }
            if (allowed) await Execute(); else Assert.ThrowsAsync<ServiceException>(() => Execute());
            Assert.That(called, Is.EqualTo(allowed));
        }
    }
}
