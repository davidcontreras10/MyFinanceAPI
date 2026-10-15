using System;
using System.Collections.Generic;
using System.Linq;
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
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Controllers;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public class AccountAuthorizationTest
    {
        private const string Actor = "017844b8-a92a-44b0-9faf-e4e7230959b1";
        private const string ApprovedOwner = "117844b8-a92a-44b0-9faf-e4e7230959b1";

        [TestCase(ResourceActionNames.View)]
        [TestCase(ResourceActionNames.Add)]
        [TestCase(ResourceActionNames.Edit)]
        [TestCase(ResourceActionNames.Delete)]
        public async Task OwnerPolicyReturnsScopeOnlyWhenEveryTargetIsOwned(ResourceActionNames action)
        {
            var owned = new AccountAccessTargets
            {
                AccountIds = new[] { 1, 2 }, AccountPeriodIds = new[] { 11 },
                AccountGroupIds = new[] { 21 }, SpendTypeIds = new[] { 31 }
            };
            var service = Service((owner, targets) => owner == Guid.Parse(Actor) &&
                targets.AccountIds.All(owned.AccountIds.Contains) && targets.AccountPeriodIds.All(owned.AccountPeriodIds.Contains) &&
                targets.AccountGroupIds.All(owned.AccountGroupIds.Contains) && targets.SpendTypeIds.All(owned.SpendTypeIds.Contains));
            var result = await service.AuthorizeAsync(Actor, action, owned);
            Assert.That(result.IsAuthorized, Is.True);
            Assert.That(result.Scope.OwnerUserId, Is.EqualTo(Actor));

            foreach (var bad in new[]
            {
                owned with { AccountIds = new[] { 1, 99 } },
                owned with { AccountPeriodIds = new[] { 11, 99 } },
                owned with { AccountGroupIds = new[] { 99 } },
                owned with { SpendTypeIds = new[] { 99 } }
            })
            {
                var denied = await service.AuthorizeAsync(Actor, action, bad);
                Assert.That(denied.IsAuthorized, Is.False);
                Assert.That(denied.Scope, Is.Null);
            }
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("invalid")]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        public async Task InvalidIdentityFailsBeforeRepository(string identity)
        {
            var result = await NoRepository().AuthorizeAsync(identity, ResourceActionNames.View, new AccountAccessTargets());
            Assert.That(result.IsAuthorized, Is.False);
        }

        [Test]
        public async Task InvalidActionsAndTargetShapesFailClosed()
        {
            foreach (var action in new[] { ResourceActionNames.Unknown, ResourceActionNames.EditSensitive, (ResourceActionNames)99 })
                Assert.That((await NoRepository().AuthorizeAsync(Actor, action, new AccountAccessTargets())).IsAuthorized, Is.False);
            foreach (var targets in new[]
            {
                null, new AccountAccessTargets { AccountIds = null }, new AccountAccessTargets { AccountIds = new[] { 0 } },
                new AccountAccessTargets { AccountPeriodIds = new[] { -1 } }, new AccountAccessTargets { AccountGroupIds = null },
                new AccountAccessTargets { SpendTypeIds = new[] { 0 } }
            })
                Assert.That((await NoRepository().AuthorizeAsync(Actor, ResourceActionNames.View, targets)).IsAuthorized, Is.False);
            Assert.Throws<ArgumentException>(() => new AccountAccessScope(Guid.Empty));
        }

        [Test]
        public async Task ListsGetExplicitOwnerScopeWithoutRequestedTargets()
        {
            var result = await Service((owner, targets) =>
                owner == Guid.Parse(Actor) && targets.AccountIds.Count == 0 && targets.AccountPeriodIds.Count == 0)
                .AuthorizeAsync(Actor, ResourceActionNames.View, new AccountAccessTargets());
            Assert.That(result.Scope.OwnerUserId, Is.EqualTo(Actor));
        }

        private static readonly string[] Endpoints =
        {
            "hint-get", "hint-put", "currencies", "delete", "delete-legacy", "add", "notes", "finance", "excel",
            "summary", "user", "list", "group", "details", "include", "add-form", "positions", "edit"
        };

        [TestCaseSource(nameof(Endpoints))]
        public void EveryEndpointStopsBeforeOperationOnDenial(string endpoint)
        {
            var calls = 0;
            var controller = Controller((identity, action, targets) =>
            {
                Assert.That(identity, Is.EqualTo(Actor));
                Assert.That(action, Is.EqualTo(ActionFor(endpoint)));
                calls++;
                return new AccountAuthorizationResult(null);
            });
            var ex = Assert.ThrowsAsync<ServiceException>(() => Invoke(controller, endpoint));
            Assert.That(ex.StatusCode, Is.EqualTo(endpoint is "hint-get" or "hint-put" ? HttpStatusCode.NotFound : HttpStatusCode.Forbidden));
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCaseSource(nameof(Endpoints))]
        public void EveryEndpointRequiresIdentity(string endpoint)
        {
            var controller = Controller((identity, action, targets) => throw new AssertionException("Authorization called without identity."));
            controller.HttpContext.User = new ClaimsPrincipal();
            Assert.ThrowsAsync<UnauthorizedAccessException>(() => Invoke(controller, endpoint));
        }

        [Test]
        public void MissingAuthorizationResultFailsClosed()
        {
            var ex = Assert.ThrowsAsync<ServiceException>(() => Controller((identity, action, targets) => null).GetAccountDetailsViewModel());
            Assert.That(ex.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void HintInvalidAccountIdRetainsBadRequest(int accountId)
        {
            var controller = Controller((identity, action, targets) => throw new AssertionException("Invalid target reached authorization."));
            var read = Assert.ThrowsAsync<ServiceException>(() => controller.GetAiClassificationHint(accountId));
            var write = Assert.ThrowsAsync<ServiceException>(() => controller.UpdateAiClassificationHint(accountId, new ClientAccountAiClassificationHint()));
            Assert.That(read.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(write.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DefaultGroupSelectorIsNotTreatedAsARecordTarget(int groupId)
        {
            var controller = Controller((identity, action, targets) =>
            {
                Assert.That(targets.AccountGroupIds, Is.Empty);
                return new AccountAuthorizationResult(null);
            });
            Assert.ThrowsAsync<ServiceException>(() => controller.GetAccountDetailsViewModelAsync(groupId));
        }

        [TestCase("hint-get")]
        [TestCase("notes")]
        [TestCase("positions")]
        [TestCase("add")]
        [TestCase("edit")]
        [TestCase("list")]
        [TestCase("details")]
        [TestCase("finance")]
        public async Task OperationsReceiveApprovedOwnerInsteadOfReconstructingIdentity(string endpoint)
        {
            var calls = 0;
            object Operation(MethodInfo method, object[] args)
            {
                calls++;
                var owner = endpoint == "details" ? args[1] : args[0];
                Assert.That(owner, Is.EqualTo(ApprovedOwner));
                return endpoint switch
                {
                    "hint-get" => Task.FromResult(new AiClassifiableAccount(1, "Owned", null)),
                    "notes" => Task.FromResult((AccountNotes)args[1]),
                    "positions" => Task.FromResult<IEnumerable<ItemModified>>(Array.Empty<ItemModified>()),
                    "list" => Task.FromResult<IReadOnlyCollection<AccountDetailsPeriodViewModel>>(Array.Empty<AccountDetailsPeriodViewModel>()),
                    "details" => Array.Empty<AccountDetailsInfoViewModel>(),
                    _ => Task.CompletedTask
                };
            }
            var controller = Controller((identity, action, targets) => new AccountAuthorizationResult(new AccountAccessScope(Guid.Parse(ApprovedOwner))),
                Operation, (method, args) =>
                {
                    Assert.That(args[1], Is.EqualTo(ApprovedOwner));
                    calls++;
                    return Task.FromResult<IReadOnlyCollection<AccountFinanceViewModel>>(Array.Empty<AccountFinanceViewModel>());
                });
            await Invoke(controller, endpoint);
            Assert.That(calls, Is.EqualTo(1));
        }

        [TestCase("add")]
        [TestCase("edit")]
        [TestCase("finance")]
        [TestCase("positions")]
        public void ChecksRequestedReferencesAndEntireBatch(string endpoint)
        {
            var controller = Controller((identity, action, targets) =>
            {
                if (endpoint is "add" or "edit")
                {
                    Assert.That(targets.AccountIds, Does.Contain(2));
                    Assert.That(targets.AccountGroupIds, Is.EqualTo(new[] { 21 }));
                    Assert.That(targets.SpendTypeIds, Is.EqualTo(new[] { 31 }));
                }
                else if (endpoint == "finance")
                    Assert.That(targets.AccountPeriodIds, Is.EqualTo(new[] { 11, 12 }));
                else
                    Assert.That(targets.AccountIds, Is.EqualTo(new[] { 1, 2 }));
                return new AccountAuthorizationResult(null);
            });
            Assert.ThrowsAsync<ServiceException>(() => Invoke(controller, endpoint));
        }

        [Test]
        public void EditIgnoresReferencesForFieldsNotBeingUpdated()
        {
            var controller = Controller((identity, action, targets) =>
            {
                Assert.That(targets.AccountIds, Is.EqualTo(new[] { 1 }));
                Assert.That(targets.AccountGroupIds, Is.Empty);
                Assert.That(targets.SpendTypeIds, Is.Empty);
                return new AccountAuthorizationResult(null);
            });
            Assert.ThrowsAsync<ServiceException>(() => controller.UpdateAccount(new ClientEditAccount
            {
                AccountId = 1, AccountGroupId = 99, SpendTypeId = 99,
                AccountIncludes = new[] { new ClientAccountInclude { AccountIncludeId = 99 } },
                EditAccountFields = new[] { AccountFiedlds.AccountName }
            }));
        }

        private static ResourceActionNames ActionFor(string endpoint) => endpoint switch
        {
            "delete" or "delete-legacy" => ResourceActionNames.Delete,
            "add" or "add-form" => ResourceActionNames.Add,
            "notes" or "hint-put" or "positions" or "edit" => ResourceActionNames.Edit,
            _ => ResourceActionNames.View
        };

        private static async Task Invoke(AccountsController controller, string endpoint)
        {
            var links = new[] { new ClientAccountInclude { AccountIncludeId = 2, AccountId = 999 } };
            var periods = new[] { new ClientAccountFinanceViewModel { AccountPeriodId = 11 }, new ClientAccountFinanceViewModel { AccountPeriodId = 12 } };
            switch (endpoint)
            {
                case "hint-get": await controller.GetAiClassificationHint(1); break;
                case "hint-put": await controller.UpdateAiClassificationHint(1, new ClientAccountAiClassificationHint()); break;
                case "currencies": await controller.GetAccountsByCurrenciesAsync(new[] { 1 }); break;
                case "delete": await controller.DeleteAccountV2(1); break;
#pragma warning disable CS0618
                case "delete-legacy": await controller.DeleteAccount(1); break;
#pragma warning restore CS0618
                case "add": await controller.AddAccount(new ClientAddAccount { AccountGroupId = 21, SpendTypeId = 31, AccountIncludes = links }); break;
                case "notes": await controller.UpdateAccountNotes(1, new AccountNotes()); break;
                case "finance": await controller.GetAccountFinanceViewModel(periods); break;
                case "excel": await controller.GetExcelAccountFinanceViewModel(periods); break;
                case "summary": await controller.GetAccountFinanceSummaryViewModel(); break;
                case "user": await controller.GetAccountsByUserId(); break;
                case "list": await controller.GetAccountDetailsViewModel(); break;
                case "group": await controller.GetAccountDetailsViewModelAsync(21); break;
                case "details": await controller.GetAccountDetailsInfoViewModel(new[] { 1 }); break;
                case "include": await controller.GetAccountIncludeViewModel(1); break;
                case "add-form": await controller.GetAddAccountViewModel(); break;
                case "positions": await controller.UpdateAccountPositions(new[] { new ClientAccountPosition { AccountId = 1 }, new ClientAccountPosition { AccountId = 2 } }); break;
                case "edit": await controller.UpdateAccount(new ClientEditAccount
                {
                    AccountId = 1, AccountGroupId = 21, SpendTypeId = 31, AccountIncludes = links,
                    EditAccountFields = new[] { AccountFiedlds.AccountGroupId, AccountFiedlds.SpendTypeId, AccountFiedlds.AccountIncludes }
                }); break;
                default: throw new AssertionException("Unknown endpoint.");
            }
        }

        private static AccountAuthorizationService NoRepository() => Service((owner, targets) => throw new AssertionException("Repository called for invalid authorization."));
        private static AccountAuthorizationService Service(Func<Guid, AccountAccessTargets, bool> check) =>
            new AccountAuthorizationService(Proxy<IAccountAccessRepository>((method, args) => Task.FromResult(check((Guid)args[0], (AccountAccessTargets)args[1]))));

        private static AccountsController Controller(Func<string, ResourceActionNames, AccountAccessTargets, AccountAuthorizationResult> authorize,
            Func<MethodInfo, object[], object> operation = null, Func<MethodInfo, object[], object> finance = null)
        {
            object Denied(MethodInfo method, object[] args) => throw new AssertionException("Protected operation executed before authorization.");
            return new AccountsController(Proxy<IAccountService>(operation ?? Denied), Proxy<IAccountFinanceService>(finance ?? Denied),
                Proxy<IAccountAuthorizationService>((method, args) => Task.FromResult(authorize((string)args[0], (ResourceActionNames)args[1], (AccountAccessTargets)args[2]))))
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
            var result = DispatchProxy.Create<T, Stub>();
            ((Stub)(object)result).Handler = handler;
            return result;
        }

        public class Stub : DispatchProxy
        {
            public Func<MethodInfo, object[], object> Handler { get; set; }
            protected override object Invoke(MethodInfo method, object[] args) => Handler(method, args);
        }
    }
}
