using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UserAuthorizationTest
    {
        private const string Actor = "017844b8-a92a-44b0-9faf-e4e7230959b1";
        private const string Other = "117844b8-a92a-44b0-9faf-e4e7230959b1";

        [TestCase(ResourceAccesLevels.Self, Actor, true)]
        [TestCase(ResourceAccesLevels.Self, Other, false)]
        [TestCase(ResourceAccesLevels.Any, Other, true)]
        [TestCase(ResourceAccesLevels.Unknown, Actor, false)]
        [TestCase(ResourceAccesLevels.AddRegular, Other, false)]
        public async Task ChecksAccessLevel(ResourceAccesLevels level, string target, bool expected)
        {
            Assert.That(await Service(level).IsAuthorizedAsync(Actor, new[] { target }, new[] { ResourceActionNames.View }), Is.EqualTo(expected));
        }

        [Test]
        public async Task OwnedRequiresEveryTargetToBeOwned()
        {
            var service = Service(ResourceAccesLevels.Owned, Other);
            Assert.That(await service.IsAuthorizedAsync(Actor, new[] { Other }, new[] { ResourceActionNames.View }), Is.True);
            Assert.That(await service.IsAuthorizedAsync(Actor, new[] { Other, Actor }, new[] { ResourceActionNames.View }), Is.False);
            Assert.That(await Service(ResourceAccesLevels.Owned).IsAuthorizedAsync(Actor, new[] { Other }, new[] { ResourceActionNames.View }), Is.False);
        }

        [Test]
        public async Task RequiresAllRequestedActionsAndDeniesMissingPermissions()
        {
            Assert.That(await Service(ResourceAccesLevels.Any).IsAuthorizedAsync(Actor, new[] { Actor },
                new[] { ResourceActionNames.View, ResourceActionNames.Edit }), Is.False);
            Assert.That(await Service(ResourceAccesLevels.Any).IsAuthorizedAsync(Actor, new[] { Actor },
                new[] { ResourceActionNames.EditSensitive }), Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidIdentityOrTargetCannotUseAnyAccess(string id)
        {
            var service = Service(ResourceAccesLevels.Any);
            Assert.That(await service.IsAuthorizedAsync(id, new[] { Other }, new[] { ResourceActionNames.View }), Is.False);
            Assert.That(await service.IsAuthorizedAsync(Actor, new[] { id }, new[] { ResourceActionNames.View }), Is.False);
        }

        [Test]
        public async Task MissingTargetsOrActionsFailClosed()
        {
            var service = Service(ResourceAccesLevels.Any);
            Assert.That(await service.IsAuthorizedAsync(Actor, Array.Empty<string>(), new[] { ResourceActionNames.View }), Is.False);
            Assert.That(await service.IsAuthorizedAsync(Actor, null, new[] { ResourceActionNames.View }), Is.False);
            Assert.That(await service.IsAuthorizedAsync(Actor, new[] { Actor }, Array.Empty<ResourceActionNames>()), Is.False);
            Assert.That(await service.IsAuthorizedAsync(Actor, new[] { Actor }, new[] { ResourceActionNames.Unknown }), Is.False);
        }

        [TestCase(ResourceActionNames.View)]
        [TestCase(ResourceActionNames.Edit)]
        [TestCase(ResourceActionNames.EditSensitive)]
        public async Task ControllerDenialNeverInvokesOperation(ResourceActionNames action)
        {
            var controller = Controller(false, action);
            ActionResult result = action switch
            {
                ResourceActionNames.View => (await controller.GetUserById(Other)).Result,
                ResourceActionNames.Edit => (await controller.UpdateUser(Other, new ClientEditUser())).Result,
                _ => (await controller.SetUserPassword(new SetPassword { UserId = Other })).Result
            };
            Assert.That(result, Is.TypeOf<StatusCodeResult>());
            Assert.That(((StatusCodeResult)result).StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task ControllerForwardsApprovedTargetInsteadOfBodyUser()
        {
            var called = false;
            var controller = Controller(true, ResourceActionNames.Edit, (method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IUsersService.UpdateUserAsync)));
                Assert.That(((ClientEditUser)args[1]).UserId, Is.EqualTo(Other));
                called = true;
                return Task.FromResult(true);
            });
            var result = await controller.UpdateUser(Other, new ClientEditUser { UserId = Actor });
            Assert.That(result.Value, Is.True);
            Assert.That(called, Is.True);
        }

        [Test]
        public void MissingIdentityStopsBeforeAuthorization()
        {
            var controller = Controller(false, ResourceActionNames.View);
            controller.HttpContext.User = new ClaimsPrincipal();
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetUserById(Other));
        }

        private static UserAuthorizeService Service(ResourceAccesLevels level, params string[] owned)
        {
            var access = Proxy<IAuthorizationDataRepository>((method, args) =>
            {
                Assert.That(args[0], Is.EqualTo(Actor));
                Assert.That(args[1], Is.EqualTo(ApplicationResources.Users));
                IEnumerable<UserAssignedAccess> grants = (ResourceActionNames)args[2] == ResourceActionNames.View
                    ? new[] { new UserAssignedAccess { ResourceAccesLevel = level } }
                    : Array.Empty<UserAssignedAccess>();
                return Task.FromResult(grants);
            });
            var users = Proxy<IUserRespository>((method, args) =>
                Task.FromResult<IEnumerable<AppUser>>(owned.Select(id => new AppUser { UserId = Guid.Parse(id) }).ToArray()));
            return new UserAuthorizeService(access, users);
        }

        private static UsersController Controller(bool allowed, ResourceActionNames action, Func<MethodInfo, object[], object> operation = null)
        {
            var users = Proxy<IUsersService>(operation ?? ((method, args) => throw new AssertionException("Denied operation executed.")));
            var authorization = Proxy<IUserAuthorizeService>((method, args) =>
            {
                Assert.That(args[0], Is.EqualTo(Actor));
                Assert.That((IEnumerable<string>)args[1], Is.EqualTo(new[] { Other }));
                Assert.That((IEnumerable<ResourceActionNames>)args[2], Is.EqualTo(new[] { action }));
                return Task.FromResult(allowed);
            });
            return new UsersController(users, authorization)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Actor) }, "Test"))
                    }
                }
            };
        }

        private static T Proxy<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = DispatchProxy.Create<T, Stub>();
            ((Stub)(object)proxy).Handler = handler;
            return proxy;
        }

        public class Stub : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
