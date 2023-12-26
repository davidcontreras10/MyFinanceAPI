using EFDataAccess.Extensions;
using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountPeriod = EFDataAccess.Models.AccountPeriod;

namespace EFDataAccess.Repositories
{
	public class EFAccountRepository : BaseEFRepository, IAccountRepository
	{
		private readonly ILogger<EFAccountRepository> _logger;

		public EFAccountRepository(MyFinanceContext context, ILogger<EFAccountRepository> logger) : base(context)
		{
			_logger = logger;
		}

		public void AddAccount(string userId, ClientAddAccount clientAddAccount)
		{
			var efAccount = new Account
			{
				AccountGroupId = clientAddAccount.AccountGroupId,
				Name = clientAddAccount.AccountName,
				PeriodDefinitionId = clientAddAccount.PeriodDefinitionId,
				CurrencyId = clientAddAccount.CurrencyId,
				BaseBudget = clientAddAccount.BaseBudget,
				HeaderColor = CreateFrontStyleDataJson(clientAddAccount.HeaderColor),
				AccountTypeId = (int)clientAddAccount.AccountTypeId,
				DefaultSpendTypeId = clientAddAccount.SpendTypeId,
				FinancialEntityId = clientAddAccount.FinancialEntityId,
				UserId = new Guid(userId)
			};

			Context.Account.Add(efAccount);
			var efAccoountIncludes = clientAddAccount.AccountIncludes != null && clientAddAccount.AccountIncludes.Any()
				? clientAddAccount.AccountIncludes.Select(acci => new AccountInclude
				{
					Account = efAccount,
					AccountIncludeId = acci.AccountIncludeId,
					CurrencyConverterMethodId = acci.CurrencyConverterMethodId
				})
				: null;
			if (efAccoountIncludes != null)
			{
				Context.AccountInclude.AddRange(efAccoountIncludes);
			}

			Context.SaveChanges();
		}

		public void DeleteAccount(string userId, int accountId)
		{
			var autoTrxIds = Context.AutomaticTask.Where(at => at.AccountId == accountId).Select(x => x.AutomaticTaskId);
			Context.ExecutedTask.RemoveWhere(x => autoTrxIds.Contains(x.AutomaticTaskId));
			Context.SpInTrxDef.RemoveWhere(x => autoTrxIds.Contains(x.SpInTrxDefId));
			Context.TransferTrxDef.RemoveWhere(x => autoTrxIds.Contains(x.TransferTrxDefId));
			Context.AutomaticTask.RemoveWhere(x => autoTrxIds.Contains(x.AutomaticTaskId));

			Context.UserBankSummaryAccount.RemoveWhere(x => x.AccountId == accountId);

			var accountPeriods = Context.AccountPeriod.Where(x => x.AccountId == accountId).ToList();
			Context.SpendOnPeriod.RemoveWhere(x => accountPeriods.Any(accp => accp.AccountPeriodId == x.AccountPeriodId));
			Context.AccountPeriod.RemoveRange(accountPeriods);

			Context.AccountInclude.RemoveWhere(x => x.AccountIncludeId == accountId || x.AccountId == accountId);
			Context.Account.RemoveWhere(x => x.AccountId == accountId);
			Context.SaveChanges();
		}

		public IEnumerable<AccountBasicPeriodInfo> GetAccountBasicInfoByAccountId(IEnumerable<int> accountIds)
		{
			return Context.AccountPeriod.AsNoTracking()
				.Where(accp => accp.AccountId != null && accountIds.Contains(accp.AccountId.Value))
				.Include(x => x.Account)
				.Select(accp => new AccountBasicPeriodInfo
			{
				AccountId = accp.AccountId.Value,
				AccountName = accp.Account.Name,
				MaxDate = accp.EndDate.Value,
				MinDate = accp.InitialDate.Value,
			});
		}

		public async Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsPeriodViewModelAsync(string userId, DateTime dateTime)
		{
			try
			{
				var accounts = await Context.Account.AsNoTracking()
					.Where(acc => new Guid(userId) == acc.UserId)
					.Include(x => x.AccountPeriod)
					.ToListAsync();
				FilterAccountPeriodByDate(accounts, dateTime);
				var viewModels = new List<AccountDetailsPeriodViewModel>();
				foreach (var acc in accounts)
				{
					var currentPeriod = acc.AccountPeriod.FirstOrDefault();
					viewModels.Add(new AccountDetailsPeriodViewModel
					{
						AccountGroupId = acc.AccountGroupId ?? 0,
						AccountId = acc.AccountId,
						AccountName = acc.Name,
						AccountPeriodId = currentPeriod != null ? currentPeriod.AccountPeriodId : 0,
						AccountPosition = acc.Position ?? 0,
						GlobalOrder = acc.Position ?? 0
					});
				}

				return viewModels;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetAccountDetailsPeriodViewModelAsync");
				throw;
			}
		}

		public async Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(string userId, int? accountGroupId)
		{
			var accountGroups = await GetAccountGroupAsync(userId, accountGroupId);
			var accountGroupIds = accountGroups.Select(accg => accg.AccountGroupId);
			if (accountGroups == null || !accountGroups.Any())
			{
				return new AccountMainViewModel();
			}

			var guidUserId = new Guid(userId);
			var accountDetailsViewModels = Context.Account.
				Where(acc =>
				accountGroupIds.Contains(acc.AccountGroupId.Value)
				).Select(acc => new AccountDetailsViewModel
				{
					AccountGroupId = acc.AccountGroupId ?? 0,
					AccountId = acc.AccountId,
					AccountName = acc.Name,
					AccountPosition = acc.Position ?? 0,
					AccountStyle = CreateFrontStyleData(acc.HeaderColor),
					BaseBudget = acc.BaseBudget ?? 0,
					GlobalOrder = acc.Position ?? 0
				});

			var accountGroupViewModels = Context.AccountGroup
				.Where(accg => accg.UserId == guidUserId)
				.Select(accg => new AccountGroupViewModel
				{
					AccountGroupId = accg.AccountGroupId,
					AccountGroupDisplayValue = accg.DisplayValue,
					AccountGroupName = accg.AccountGroupName,
					AccountGroupPosition = accg.AccountGroupPosition ?? 0,
					IsSelected = accountGroupIds.Contains(accg.AccountGroupId)
				}).OrderBy(accg => accg.AccountGroupPosition);
			return new AccountMainViewModel
			{
				AccountDetailsViewModels = accountDetailsViewModels,
				AccountGroupViewModels = accountGroupViewModels
			};
		}

		public IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(IEnumerable<int> accountIds, string userId)
		{
			var userGuid = new Guid(userId);
			var efAccountTypes = Context.AccountType.ToList();
			var efFinancialEntityViewModels = Context.FinancialEntity
				.Where(f => !EF.Functions.Like(f.Name, "%default%"))
				.ToList();
			var currencyConverters = Context.CurrencyConverter.Include(c => c.CurrencyConverterMethod).ThenInclude(x => x.FinancialEntity)
				.ToList();
			var periodTypeViewModels = Context.PeriodDefinition.Include(pd => pd.PeriodType).Where(pd => pd.PeriodType != null).Select(pd =>
			new PeriodTypeViewModel
			{
				CuttingDate = pd.CuttingDate,
				PeriodDefinitionId = pd.PeriodDefinitionId,
				PeriodTypeId = pd.PeriodTypeId,
				PeriodTypeName = pd.PeriodType.Name
			});

			var userAccounts = Context.Account.Where(acc => acc.UserId == userGuid);
			var queryAccounts = Context.Account
				.Where(acc => accountIds.Contains(acc.AccountId))
				.Include(acc => acc.AccountIncludeAccount)
				.ToList()
				;
			var efCurrencies = Context.Currency.ToList();
			var efAccountGroups = Context.AccountGroup.ToList();
			var acc = queryAccounts.Select(acc => new AccountDetailsInfoViewModel
			{
				AccountName = acc.Name,
				AccountPosition = acc.Position ?? 0,
				AccountGroupId = acc.AccountGroupId ?? 0,
				AccountId = acc.AccountId,
				SpendTypeViewModels = Context.UserSpendType.Where(ust => ust.UserId == userGuid)
					.Include(x => x.SpendType)
					.Select(x => x.SpendType.ToSpendTypeViewModel(acc.DefaultSpendTypeId)),
				AccountTypeViewModels = efAccountTypes.Select(acct => new AccountTypeViewModel
				{
					AccountTypeId = acct.AccountTypeId,
					AccountTypeName = acct.AccountTypeName,
					IsDefault = acct.AccountTypeId == acc.AccountTypeId
				}),
				PeriodTypeViewModels = periodTypeViewModels,
				FinancialEntityViewModels = efFinancialEntityViewModels.Select(f =>
				new FinancialEntityViewModel
				{
					FinancialEntityId = f.FinancialEntityId,
					FinancialEntityName = f.Name,
					IsDefault = f.FinancialEntityId == acc.FinancialEntityId
				}),
				AccountIncludeViewModels = GetPossibleAccountIncludes(acc.AccountIncludeAccount.ToList(), userAccounts.ToList(), currencyConverters, acc),
				CurrencyViewModels = efCurrencies.Select(c => new CurrencyViewModel
				{
					CurrencyId = c.CurrencyId,
					CurrencyName = c.Name,
					Symbol = c.Symbol,
					Isdefault = c.CurrencyId == acc.CurrencyId
				}),
				AccountGroupViewModels = efAccountGroups.Select(accg => new AccountGroupViewModel
				{
					AccountGroupDisplayValue = accg.DisplayValue,
					AccountGroupId = accg.AccountGroupId,
					AccountGroupName = accg.AccountGroupName,
					AccountGroupPosition = accg.AccountGroupPosition ?? 0,
					IsSelected = acc.AccountGroupId == accg.AccountGroupId
				}),
				BaseBudget = acc.BaseBudget ?? 0,
				AccountStyle = CreateFrontStyleData(acc.HeaderColor),
				GlobalOrder = acc.Position ?? 0
			}).ToList();

			return acc;
		}

		public IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel(string userId, int currencyId)
		{
			var userGuid = new Guid(userId);
			var userAccounts = Context.Account.Where(acc => acc.UserId == userGuid);
			var currencyConverterMethods = Context.CurrencyConverterMethod.Include(c => c.CurrencyConverter).Include(c => c.FinancialEntity);
			var applicable = new List<AccountIncludeViewModel>();
			foreach (var account in userAccounts)
			{
				var ccMethods = currencyConverterMethods.Where(ccm =>
					ccm.CurrencyConverter.CurrencyIdOne == currencyId && ccm.CurrencyConverter.CurrencyIdTwo == account.CurrencyId);
				var acci = new AccountIncludeViewModel
				{
					AccountId = account.AccountId,
					AccountName = account.Name,
					MethodIds = ccMethods.Select(ccm => new MethodId
					{
						Id = ccm.CurrencyConverterMethodId,
						Name = ccm.Name,
						IsDefault = ccm.IsDefault ?? false
					})
				};

				applicable.Add(acci);
			}

			return applicable;
		}

		public IEnumerable<AccountPeriodBasicInfo> GetAccountPeriodBasicInfo(IEnumerable<int> accountPeriodIds)
		{
			return Context.AccountPeriod.AsNoTracking()
				.Where(accp => accountPeriodIds.Contains(accp.AccountPeriodId))
				.Include(accp => accp.Account)
				.Select(accp => new AccountPeriodBasicInfo
				{
					AccountPeriodId = accp.AccountPeriodId,
					AccountId = accp.AccountId ?? 0,
					AccountName = accp.Account.Name,
					InitialDate = accp.InitialDate ?? new DateTime(),
					EndDate = accp.EndDate ?? new DateTime(),
					Budget = accp.Budget ?? 0,
					UserId = accp.Account.UserId.ToString()
				});
		}

		public AccountPeriodBasicInfo GetAccountPeriodInfoByAccountIdDateTime(int accountId, DateTime dateTime)
		{
			var account = Context.Account
				.Where(acc => acc.AccountId == accountId)
				.Include(acc => acc.AccountPeriod
					.Where(accp => dateTime >= accp.InitialDate && dateTime < accp.EndDate)).FirstOrDefault();
			var accountPeriod = account?.AccountPeriod?.FirstOrDefault();
			if (accountPeriod == null)
			{
				return null;
			}

			return new AccountPeriodBasicInfo
			{
				AccountId = account.AccountId,
				AccountName = account.Name,
				AccountPeriodId = accountPeriod.AccountPeriodId,
				InitialDate = accountPeriod.InitialDate ?? new DateTime(),
				EndDate = accountPeriod.EndDate ?? new DateTime(),
				Budget = accountPeriod.Budget ?? 0,
				UserId = account.UserId.ToString()
			};
		}

		public async Task<AccountPeriodBasicInfo> GetAccountPeriodInfoByAccountIdDateTimeAsync(int accountId, DateTime dateTime)
		{
			var account = await Context.Account.AsNoTracking()
				.Where(acc => acc.AccountId == accountId)
				.Include(acc => acc.AccountPeriod)
				.FirstOrDefaultAsync();
			FilterAccountPeriodByDate(new[] { account }, dateTime);
			var accountPeriod = account?.AccountPeriod?.FirstOrDefault();
			if (accountPeriod == null)
			{
				return null;
			}

			return new AccountPeriodBasicInfo
			{
				AccountId = account.AccountId,
				AccountName = account.Name,
				AccountPeriodId = accountPeriod.AccountPeriodId,
				InitialDate = accountPeriod.InitialDate ?? new DateTime(),
				EndDate = accountPeriod.EndDate ?? new DateTime(),
				Budget = accountPeriod.Budget ?? 0,
				UserId = account.UserId.ToString()
			};
		}

		public UserAccountsViewModel GetAccountsByUserId(string userId)
		{
			var dateTime = DateTime.UtcNow;
			var userGuid = new Guid(userId);
			var userAccounts = Context.Account
				.Include(acc => acc.Currency)
				.Include(acc => acc.PeriodDefinition)
				.Include(acc => acc.AccountPeriod)
				.Include(acc => acc.AccountGroup)
				.Where(acc => acc.UserId == userGuid)
				.ToList();
			UpdateCurrentCurrentAccountPeriods(userAccounts, dateTime);
			Context.SaveChanges();
			var accountGroupViewModels = new List<AccountGroupMainViewViewModel>();
			foreach (var acc in userAccounts)
			{
				var accgViewModel = accountGroupViewModels.FirstOrDefault(accg => accg.AccountGroupId == acc.AccountGroupId);
				if (accgViewModel == null)
				{
					var accountGroup = acc.AccountGroup;
					accgViewModel = new AccountGroupMainViewViewModel
					{
						AccountGroupDisplayValue = accountGroup.DisplayValue,
						AccountGroupId = accountGroup.AccountGroupId,
						AccountGroupName = accountGroup.AccountGroupName,
						AccountGroupPosition = accountGroup.AccountGroupPosition ?? 0,
						IsSelected = accountGroup.DisplayDefault ?? false,
						Accounts = new List<FullAccountInfoViewModel>()
					};

					accountGroupViewModels.Add(accgViewModel);
				}

				var newAcc = new FullAccountInfoViewModel
				{
					AccountPeriods = acc.AccountPeriod.Select(accp => new MyFinanceModel.AccountPeriod
					{
						AccountId = accp.AccountId ?? 0,
						AccountPeriodId = accp.AccountPeriodId,
						Budget = accp.Budget ?? 0,
						EndDate = accp.EndDate ?? new DateTime(),
						InitialDate = accp.InitialDate ?? new DateTime(),
						UserId = acc.UserId.ToString()
					}).ToList(),
					AccountId = acc.AccountId,
					AccountName = acc.Name,
					CurrencyId = acc.CurrencyId ?? 0,
					CurrencyName = acc.Currency.Name,
					CurrentPeriodId = AccountHelpers.GetCurrentAccountPeriod(dateTime, acc)?.AccountPeriodId ?? 0,
					FrontStyle = CreateFrontStyleData(acc.HeaderColor),
					Type = (FullAccountInfoViewModel.AccountType)acc.AccountTypeId,
					Position = acc.Position ?? 0,
					NoteBody = acc.Notes?.Content,
					NoteTitle = acc.Notes?.Title,
				};

				((List<FullAccountInfoViewModel>)accgViewModel.Accounts).Add(newAcc);
			}

			foreach (var accountGroup in accountGroupViewModels)
			{
				accountGroup.Accounts = accountGroup.Accounts.OrderBy(acc => acc.Position);
			}

			return new UserAccountsViewModel
			{
				AccountGroupMainViewViewModels = accountGroupViewModels.OrderBy(g => g.AccountGroupPosition)
			};
		}

		public async Task<AddAccountViewModel> GetAddAccountViewModelAsync(string userId)
		{
			var userGuid = new Guid(userId);
			var accountTypeViewModels = await Context.AccountType.Select(acct => new AccountTypeViewModel
			{
				AccountTypeId = acct.AccountTypeId,
				AccountTypeName = acct.AccountTypeName
			}).ToListAsync();
			var userData = await Context.AppUser
				.Include(u => u.UserSpendType)
					.ThenInclude(ust => ust.SpendType)
				.Where(u => u.UserId == userGuid)
				.ToListAsync();
			var spendTypeViewModels = userData.FirstOrDefault()
				.UserSpendType
				.Select(ust => new SpendTypeViewModel
				{
					Description = ust.SpendType.Description,
					SpendTypeName = ust.SpendType.Name,
					SpendTypeId = ust.SpendTypeId,
				});

			const int initialCurrencyId = 1;
			var accountIncludeViewModels = GetAccountIncludeViewModel(userId, initialCurrencyId);
			var currencyViewModels = await Context.Currency.Select(c => new CurrencyViewModel
			{
				CurrencyId = c.CurrencyId,
				CurrencyName = c.Name,
				Symbol = c.Symbol
			}).ToListAsync();

			var financialEntityViewModels = await Context.FinancialEntity.
				Select(fe => new FinancialEntityViewModel
				{
					FinancialEntityId = fe.FinancialEntityId,
					FinancialEntityName = fe.Name
				})
				.Where(fe => !EF.Functions.Like(fe.FinancialEntityName, "%default%"))
				.ToListAsync();

			var periodTypeViewModels = await Context.PeriodDefinition
				.Include(pd => pd.PeriodType).Where(pd => pd.PeriodType != null)
				.Select(pd =>
					new PeriodTypeViewModel
					{
						CuttingDate = pd.CuttingDate,
						PeriodDefinitionId = pd.PeriodDefinitionId,
						PeriodTypeId = pd.PeriodTypeId,
						PeriodTypeName = pd.PeriodType.Name
					}
				)
				.ToListAsync();

			var accountGroupViewModels = Context.AccountGroup.Select(accg => new AccountGroupViewModel
			{
				AccountGroupDisplayValue = accg.DisplayValue,
				AccountGroupId = accg.AccountGroupId,
				AccountGroupName = accg.AccountGroupName,
				AccountGroupPosition = accg.AccountGroupPosition ?? 0
			});
			return new AddAccountViewModel
			{
				AccountGroupViewModels = accountGroupViewModels,
				AccountIncludeViewModels = accountIncludeViewModels,
				AccountName = "New account",
				AccountStyle = new FrontStyleData(),
				AccountTypeViewModels = accountTypeViewModels,
				BaseBudget = 0,
				CurrencyViewModels = currencyViewModels,
				FinancialEntityViewModels = financialEntityViewModels,
				PeriodTypeViewModels = periodTypeViewModels,
				SpendTypeViewModels = spendTypeViewModels
			};

		}

		public async Task<IEnumerable<BankAccountPeriodBasicId>> GetBankSummaryAccountsPeriodByUserIdAsync(string userId, DateTime? dateTime)
		{
			if (dateTime == null)
			{
				dateTime = DateTime.UtcNow;
			}
			var userGuid = new Guid(userId);
			var userBankAccounts = await Context.UserBankSummaryAccount
				.Where(bacc => bacc.UserId == userGuid)
				.Include(bacc => bacc.Account)
					.ThenInclude(acc => acc.FinancialEntity)
				.Include(bacc => bacc.Account)
					.ThenInclude(acc => acc.AccountPeriod)
				.ToListAsync();
			var currentAccountPeriods = userBankAccounts
				.Select(bacc => AccountHelpers.GetCurrentAccountPeriod(dateTime, bacc.Account.AccountPeriod))
				.Where(p => p != null).ToList();

			return userBankAccounts.Select(bacc => new BankAccountPeriodBasicId
			{
				AccountId = bacc.AccountId,
				AccountPeriodId = currentAccountPeriods.FirstOrDefault(accp => accp.AccountId == bacc.AccountId)?.AccountPeriodId ?? 0,
				FinancialEntityId = bacc.Account.FinancialEntityId ?? 0,
				FinancialEntityName = bacc.Account.FinancialEntity?.Name
			});
		}

		public async Task<IEnumerable<AccountViewModel>> GetOrderedAccountViewModelListAsync(IEnumerable<int> accountIds, string userId)
		{
			var userGuid = new Guid(userId);
			var count = 1;
			var viewModels = await Context.Account
				.Include(acc => acc.AccountGroup)
				.Where(acc => acc.UserId == userGuid && accountIds.Contains(acc.AccountId))
				.OrderBy(acc => acc.AccountGroup.AccountGroupPosition)
					.ThenBy(acc => acc.Position)
				.Select(acc => new AccountViewModel
				{
					AccountId = acc.AccountId,
					AccountName = acc.Name,
				})
				.ToListAsync();
			viewModels.ForEach(acc =>
			{
				acc.GlobalOrder = count++;
			});

			return viewModels;
		}

		public async Task UpdateAccountAsync(string userId, ClientEditAccount clientEditAccount)
		{
			if (!clientEditAccount.EditAccountFields.Any())
			{
				throw new Exception("No values to update");
			}

			var query = Context.Account.Where(acc => acc.AccountId == clientEditAccount.AccountId);
			if (clientEditAccount.Contains(AccountFiedlds.AccountIncludes))
			{
				query = query.Include(acc => acc.AccountIncludeAccount);

			}
			if (clientEditAccount.Contains(AccountFiedlds.FinancialEntityId))
			{
				query = query.Include(acc => acc.AccountIncludeAccount)
						.ThenInclude(acci => acci.AccountIncludeNavigation);
			}

			var account = query.FirstOrDefault();
			if (account == null)
			{
				throw new Exception("Account does not exist");
			}
			if (clientEditAccount.Contains(AccountFiedlds.AccountName))
			{
				account.Name = clientEditAccount.AccountName;
			}
			if (clientEditAccount.Contains(AccountFiedlds.BaseBudget))
			{
				account.BaseBudget = clientEditAccount.BaseBudget;
				var latestAccountPeriod = await Context.AccountPeriod
					.Where(accp => accp.AccountId == account.AccountId)
					.OrderByDescending(accp => accp.EndDate)
					.FirstOrDefaultAsync();
				if (latestAccountPeriod != null)
				{
					latestAccountPeriod.Budget = clientEditAccount.BaseBudget;
				}
			}

			if (clientEditAccount.Contains(AccountFiedlds.HeaderColor))
			{
				account.HeaderColor = CreateFrontStyleDataJson(clientEditAccount.HeaderColor);
			}

			if (clientEditAccount.Contains(AccountFiedlds.AccountTypeId))
			{
				account.AccountTypeId = (int)clientEditAccount.AccountTypeId;
			}
			if (clientEditAccount.Contains(AccountFiedlds.SpendTypeId))
			{
				account.DefaultSpendTypeId = clientEditAccount.SpendTypeId;
			}
			if (clientEditAccount.Contains(AccountFiedlds.AccountGroupId))
			{
				account.AccountGroupId = clientEditAccount.AccountGroupId;
			}
			if (clientEditAccount.Contains(AccountFiedlds.FinancialEntityId))
			{
				account.FinancialEntityId = clientEditAccount.FinancialEntityId;
				if (account.AccountIncludeAccount.Any())
				{
					var currencyConverterMethods = await Context.CurrencyConverterMethod
						.Include(ccm => ccm.CurrencyConverter)
						.Where(ccm =>
							ccm.CurrencyConverter.CurrencyIdOne == account.CurrencyId
							&& ccm.FinancialEntityId == clientEditAccount.FinancialEntityId)
						.ToListAsync();
					foreach (var accountIncluded in account.AccountIncludeAccount)
					{
						var selectCcm = currencyConverterMethods
							.FirstOrDefault(ccm => ccm.CurrencyConverter.CurrencyIdTwo == accountIncluded.AccountIncludeNavigation.CurrencyId);
						if (selectCcm != null)
						{
							accountIncluded.CurrencyConverterMethodId = selectCcm.CurrencyConverterMethodId;
						}
					}
				}
			}

			if (clientEditAccount.Contains(AccountFiedlds.AccountIncludes))
			{
				account.AccountIncludeAccount.Clear();
				foreach (var clientAcountInclude in clientEditAccount.AccountIncludes)
				{
					account.AccountIncludeAccount.Add(new AccountInclude
					{
						AccountId = clientAcountInclude.AccountId,
						AccountIncludeId = clientAcountInclude.AccountIncludeId,
						CurrencyConverterMethodId = clientAcountInclude.CurrencyConverterMethodId
					});
				}
			}
			await Context.SaveChangesAsync();
		}

		public async Task<IEnumerable<ItemModified>> UpdateAccountPositionsAsync(string userId, IEnumerable<ClientAccountPosition> accountPositions)
		{
			try
			{
				var ids = accountPositions.Select(accp => accp.AccountId);
				var accounts = Context.Account.Where(acc => ids.Contains(acc.AccountId)).ToList();
				foreach (var acc in accounts)
				{
					var clientAccount = accountPositions.First(ap => ap.AccountId == acc.AccountId);
					acc.Position = clientAccount.Position;
				}
				await Context.SaveChangesAsync();
				return accounts.Select(acc => new ItemModified
				{
					AccountId = acc.AccountId,
					IsModified = true
				});
			}
			catch (Exception ex)
			{

				throw;
			}

		}

		#region Privates

		private async Task<IReadOnlyCollection<AccountGroup>> GetAccountGroupAsync(string userId, int? accountGroupId)
		{
			var userGuid = new Guid(userId);
			if (accountGroupId == null)
			{
				return await Context.AccountGroup
					.Where(accg => accg.UserId == userGuid)
					.ToListAsync();
			}
			if (accountGroupId < 1)
			{
				var defaultAccountGroup = await Context.AccountGroup
					.Where(accg => accg.UserId == userGuid)
					.OrderBy(accg => accg.AccountGroupPosition)
					.FirstOrDefaultAsync();
				return defaultAccountGroup != null ? new[] { defaultAccountGroup } : Array.Empty<AccountGroup>();
			}

			return await Context.AccountGroup
				.Where(accg => accg.AccountGroupId == accountGroupId)
				.ToListAsync();
		}

		private void UpdateCurrentCurrentAccountPeriods(string userId, DateTime? currentDate)
		{
			var userGuid = new Guid(userId);
			var userAccounts = Context.Account
				.AsNoTracking()
				.Where(acc => acc.UserId == userGuid)
				.Include(acc => acc.AccountPeriod)
				.Include(acc => acc.PeriodDefinition);
			UpdateCurrentCurrentAccountPeriods(userAccounts, currentDate);
		}

		private void UpdateCurrentCurrentAccountPeriods(IEnumerable<Account> userAccounts, DateTime? currentDate)
		{
			if (currentDate == null)
			{
				currentDate = DateTime.UtcNow;
			}

			var nonCurretPeriodAccounts = userAccounts.Where(acc => AccountHelpers.GetCurrentAccountPeriod(currentDate, acc.AccountPeriod) == null);
			var nonCurretPeriodAccountsCount = nonCurretPeriodAccounts.Count();
			var newAccountPeriods = new List<AccountPeriod>();
			_logger.LogInformation($"Updating {nonCurretPeriodAccountsCount} accounts");
			foreach (var account in nonCurretPeriodAccounts)
			{
				var newPeriod = PeriodCreatorHelper.GetNewPeriodDates(account, currentDate.Value);
				newAccountPeriods.Add(new AccountPeriod
				{
					AccountId = account.AccountId,
					Budget = account.BaseBudget,
					InitialDate = newPeriod.InitialDate,
					EndDate = newPeriod.EndDate,
				});
			}

			Context.AccountPeriod.AddRange(newAccountPeriods);
		}

		private static void FilterAccountPeriodByDate(IReadOnlyCollection<Account> accounts, DateTime dateTime)
		{
			foreach (var account in accounts.Where(a => a.AccountPeriod != null))
			{
				var currentAccountPeriod = account.AccountPeriod
					.FirstOrDefault(accp => accp.InitialDate <= dateTime && dateTime < accp.EndDate);
				account.AccountPeriod = currentAccountPeriod != null 
					? new[] { currentAccountPeriod } 
					: Array.Empty<AccountPeriod>();
			}
		}

		private static IEnumerable<AccountIncludeViewModel> GetPossibleAccountIncludes(
			IReadOnlyCollection<AccountInclude> defaultAccountIncludes,
			IReadOnlyCollection<Account> userAccounts,
			IReadOnlyCollection<CurrencyConverter> currencyConverters,
			Account currentAccount
			)
		{
			var applicableUserAccounts = currentAccount != null
				? userAccounts.Where(acc => acc.AccountId != currentAccount.AccountId).ToList()
				: userAccounts;
			var currentCurrencyId = currentAccount?.CurrencyId != null ? currentAccount.CurrencyId.Value : 1;

			return GetPossibleAccountIncludes(defaultAccountIncludes, userAccounts, currencyConverters, applicableUserAccounts, currentCurrencyId);
		}

		private static IEnumerable<AccountIncludeViewModel> GetPossibleAccountIncludes(
			IReadOnlyCollection<AccountInclude> defaultAccountIncludes,
			IReadOnlyCollection<Account> userAccounts,
			IReadOnlyCollection<CurrencyConverter> currencyConverters,
			IReadOnlyCollection<Account> applicableUserAccounts,
			int currentCurrencyId
		)
		{
			var accountIncludes = new List<AccountIncludeViewModel>();
			foreach (var appAccount in applicableUserAccounts)
			{
				var appCurrencyConverters = currencyConverters
					.Where(cc => cc.CurrencyIdOne == currentCurrencyId && cc.CurrencyIdTwo == appAccount.CurrencyId).ToList();
				var accountIncludeViewModel = CreateAccountIncludeViewModel(appAccount, appCurrencyConverters, defaultAccountIncludes);
				accountIncludes.Add(accountIncludeViewModel);
			}

			return accountIncludes;
		}


		private static AccountIncludeViewModel CreateAccountIncludeViewModel(
			Account includeAccount
			, IReadOnlyCollection<CurrencyConverter> currencyConverters
			, IReadOnlyCollection<AccountInclude> defaultAccountIncludes
			)
		{
			var defaultAccountInclude = defaultAccountIncludes?.FirstOrDefault(d => d.AccountIncludeId == includeAccount.AccountId);
			var methodIds = new List<MethodId>();
			foreach (var currencyConverter in currencyConverters)
			{
				foreach (var currencyConverterMethod in currencyConverter.CurrencyConverterMethod)
				{
					methodIds.Add(new MethodId
					{
						Id = currencyConverterMethod.CurrencyConverterId,
						IsDefault = currencyConverterMethod.IsDefault ?? false,
						Name = currencyConverterMethod.Name,
						IsSelected = defaultAccountInclude != null &&
							defaultAccountInclude.CurrencyConverterMethodId == currencyConverterMethod.CurrencyConverterMethodId,
					});
				}
			}
			return new AccountIncludeViewModel
			{
				AccountId = includeAccount.AccountId,
				AccountName = includeAccount.Name,
				MethodIds = methodIds,
				IsSelected = defaultAccountInclude != null
			};
		}

		private int GetAccountNextId()
		{
			return Context.Account.Max(x => x.AccountId) + 1;
		}

		private static FrontStyleData CreateFrontStyleData(string json)
		{
			if (string.IsNullOrEmpty(json))
			{
				return new FrontStyleData();
			}
			json = json.ToUpper();
			try
			{
				var jObject = JObject.Parse(json);
				return new FrontStyleData
				{
					BorderColor = (string)jObject["borderColor".ToUpper()],
					HeaderColor = (string)jObject["headerColor".ToUpper()]
				};
			}
			catch (JsonReaderException)
			{
				return new FrontStyleData();
			}
		}

		private static string CreateFrontStyleDataJson(FrontStyleData frontStyleData)
		{
			if (frontStyleData == null)
				return "";

			var result = JsonConvert.SerializeObject(frontStyleData);
			return result;
		}

		public async Task<MyFinanceModel.ViewModel.AccountNotes> UpdateNotes(MyFinanceModel.ViewModel.AccountNotes accountNotes, int accountId)
		{
			var account = await Context.Account.Where(acc => acc.AccountId ==  accountId).FirstAsync();
			account.Notes = new Models.AccountNotes
			{
				Content = accountNotes.NoteContent,
				Title = accountNotes.NoteTitle
			};

			await Context.SaveChangesAsync();
			return accountNotes;
		}

		#endregion
	}
}
