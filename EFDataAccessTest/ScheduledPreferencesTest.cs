using System;
using System.Net;
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
using MyFinanceWebApiCore.Controllers;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class ScheduledPreferencesTest
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("Central America Standard Time")]
        [TestCase("not/a-zone")]
        public void InvalidTimezoneCannotReachPersistence(string zone)
        {
            var service = new UsersService(null, null, null);
            var error = Assert.ThrowsAsync<ServiceException>(() => service.UpdateSchedulingPreferencesAsync(Guid.NewGuid().ToString(), zone));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [TestCase("America/Costa_Rica")]
        [TestCase("Asia/Kathmandu")]
        [TestCase("America/New_York")]
        public async Task ExplicitTimezoneIsForwardedWithoutInference(string zone)
        {
            var owner = Guid.NewGuid();
            var repository = Proxy<IScheduledUserPreferencesRepository>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IScheduledUserPreferencesRepository.UpdatePreferencesAsync)));
                Assert.That(args[0], Is.EqualTo(owner)); Assert.That(args[1], Is.EqualTo(zone));
                return Task.FromResult(new ScheduledUserPreferences(zone));
            });
            Assert.That((await new UsersService(null, null, null, repository)
                .UpdateSchedulingPreferencesAsync(owner.ToString(), zone)).TimeZoneId, Is.EqualTo(zone));
        }

        [Test]
        public void DisabledHistoryPreventsPreferenceAccessAndRegistrationBeforeDatabase()
        {
            var repository = new EFScheduledScheduleRepository(null, TimeProvider.System, null, new ScheduledExecutionOptions());
            var owner = Guid.NewGuid();
            var calls = new Func<Task>[]
            {
                () => repository.GetPreferencesAsync(owner),
                () => repository.UpdatePreferencesAsync(owner, "UTC"),
                () => repository.RegisterAsync(owner, () => throw new AssertionException("Disabled registration reached"))
            };
            foreach (var call in calls)
                Assert.That(Assert.ThrowsAsync<ServiceException>(() => call()).StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task PreferenceControllerAuthorizesExactTargetAndAction(bool update, bool allowed)
        {
            var actor = Guid.NewGuid().ToString(); var target = Guid.NewGuid().ToString(); var called = false;
            var authorization = Proxy<IUserAuthorizeService>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(actor));
                Assert.That(args[1], Is.EqualTo(new[] { target }));
                Assert.That(args[2], Is.EqualTo(new[] { update ? ResourceActionNames.Edit : ResourceActionNames.View }));
                return Task.FromResult(allowed);
            });
            var operations = Proxy<IUsersService>((method, args) =>
            {
                Assert.That(allowed, Is.True);
                Assert.That(method.Name, Is.EqualTo(update ? nameof(IUsersService.UpdateSchedulingPreferencesAsync) : nameof(IUsersService.GetSchedulingPreferencesAsync)));
                Assert.That(args[0], Is.EqualTo(target));
                if (update) Assert.That(args[1], Is.EqualTo("Asia/Kathmandu"));
                called = true; return Task.FromResult(new ScheduledUserPreferences("Asia/Kathmandu"));
            });
            var controller = new UsersController(operations, authorization) { ControllerContext = Context(actor) };
            var result = update
                ? await controller.UpdateSchedulingPreferences(target, new UpdateScheduledUserPreferences { TimeZoneId = "Asia/Kathmandu" })
                : await controller.GetSchedulingPreferences(target);
            Assert.That(called, Is.EqualTo(allowed));
            if (!allowed) Assert.That(((StatusCodeResult)result.Result).StatusCode, Is.EqualTo(403));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingIdentityFailsBeforePreferenceAuthorization(bool update)
        {
            var controller = new UsersController(null, null) { ControllerContext = Context(null) };
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                if (update) await controller.UpdateSchedulingPreferences(Guid.NewGuid().ToString(), new UpdateScheduledUserPreferences { TimeZoneId = "UTC" });
                else await controller.GetSchedulingPreferences(Guid.NewGuid().ToString());
            });
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public async Task CurrentPreferenceRoutesUseAuthenticatedIdentityAndPolicy(bool update, bool allowed)
        {
            var actor = Guid.NewGuid().ToString(); var called = false;
            var authorization = Proxy<IUserAuthorizeService>((_, args) =>
            {
                Assert.That(args[0], Is.EqualTo(actor));
                Assert.That(args.Length, Is.EqualTo(1));
                return Task.FromResult(allowed);
            });
            var operations = Proxy<IUsersService>((method, args) =>
            {
                Assert.That(allowed, Is.True);
                Assert.That(method.Name, Is.EqualTo(update ? nameof(IUsersService.UpdateSchedulingPreferencesAsync) : nameof(IUsersService.GetSchedulingPreferencesAsync)));
                Assert.That(args[0], Is.EqualTo(actor));
                if (update) Assert.That(args[1], Is.EqualTo("UTC"));
                called = true;
                return Task.FromResult(new ScheduledUserPreferences("UTC"));
            });
            var controller = new UsersController(operations, authorization) { ControllerContext = Context(actor) };
            var result = update
                ? await controller.UpdateCurrentSchedulingPreferences(new UpdateScheduledUserPreferences { TimeZoneId = "UTC" })
                : await controller.GetCurrentSchedulingPreferences();
            Assert.That(called, Is.EqualTo(allowed));
            if (!allowed) Assert.That(((StatusCodeResult)result.Result).StatusCode, Is.EqualTo(403));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CurrentPreferenceRoutesRejectMissingIdentity(bool update)
        {
            var controller = new UsersController(null, null) { ControllerContext = Context(null) };
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            {
                if (update) await controller.UpdateCurrentSchedulingPreferences(new UpdateScheduledUserPreferences { TimeZoneId = "UTC" });
                else await controller.GetCurrentSchedulingPreferences();
            });
        }

        internal static ControllerContext Context(string identity) => new()
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(identity == null
                ? Array.Empty<Claim>() : new[] { new Claim(ClaimTypes.NameIdentifier, identity) }, "test")) }
        };

        internal static T Proxy<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, PreferenceProxy>();
            ((PreferenceProxy)(object)proxy).Handler = handler;
            return proxy;
        }

        public class PreferenceProxy : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
