using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Models;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Data
{
	public class AccountRepository : SqlServerBaseService, IAccountRepository
	{
		#region Constructor

		public AccountRepository(IConnectionConfig config) : base(config)
		{
		}

		#endregion

		#region Public

		public IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(IEnumerable<int> accountIds, string userId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_IDS, ServicesUtils.CreateStringCharSeparated(accountIds))
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNTS_DETAILS_LIST, parameters);
			var resultSet = ServicesUtils.CreateAccountEditViewModelResultSet(dataSet);
			var result = GetAccountDetailsViewModel(resultSet);
			return result;
		}

		public IEnumerable<AccountIncludeViewModel> GetAccountIncludeViewModel(string userId, int currencyId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, currencyId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_POSSIBLE_ACCOUNT_INCLUDE_LIST, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				throw new Exception("Invalid dataSet returned");
			}

			var resultSet = ServicesUtils.CreateGenericList(dataSet.Tables[0],
				ServicesUtils.CreateAccountIncludeInfoResultSet);
			var result = CreateAccountIncludeViewModel(resultSet);
			return result;
		}

		public Task UpdateAccountAsync(string userId, ClientEditAccount clientEditAccount)
		{
			if (!clientEditAccount.EditAccountFields.Any())
			{
				throw new Exception("No values to update");
			}

			var editFields = ServicesUtils.CreateStringCharSeparated(clientEditAccount.EditAccountFields.Select(i => (int)i));
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, clientEditAccount.AccountId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_NAME, clientEditAccount.AccountName),
				new SqlParameter(DatabaseConstants.PAR_BASE_BUDGET, clientEditAccount.BaseBudget),
				new SqlParameter(DatabaseConstants.PAR_FINANCIAL_ENTITY_ID, clientEditAccount.FinancialEntityId),
				new SqlParameter(DatabaseConstants.PAR_HEADER_COLOR,
					ServicesUtils.CreateFrontStyleDataJson(clientEditAccount.HeaderColor)),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_TYPE_ID, (int)clientEditAccount.AccountTypeId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, clientEditAccount.SpendTypeId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_INCLUDE_DATA,
					ServicesUtils.CreateClientAccountIncludeJson(clientEditAccount.AccountIncludes)),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_ID,clientEditAccount.AccountGroupId),
				new SqlParameter(DatabaseConstants.PAR_EDIT_FIELDS, editFields)
			};

			ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_EDIT, parameters);
			return Task.CompletedTask;
		}

		public Task<IEnumerable<ItemModified>> UpdateAccountPositionsAsync(string userId,
			IEnumerable<ClientAccountPosition> accountPositions)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_POSITIONS,
					ServicesUtils.CreateClientAccountPositionToJson(accountPositions))
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_POSITION_UPDATE, parameters);
			var result = ServicesUtils.CreateAccountAffected(dataSet);
			return Task.FromResult(result);
		}

		public Task<AddAccountViewModel> GetAddAccountViewModelAsync(string userId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNTS_CREATE_VIEW_MODEL, parameters);
			var resultSet = ServicesUtils.CreateAccountAddViewModelResultSet(dataSet);
			var result = CreateAddAccountViewModel(resultSet);
			return Task.FromResult(result);
		}

		public void AddAccount(string userId, ClientAddAccount clientAddAccount)
		{
			if (clientAddAccount == null)
			{
				throw new ArgumentNullException(nameof(clientAddAccount));
			}

			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_NAME, clientAddAccount.AccountName),
				new SqlParameter(DatabaseConstants.PAR_PERIOD_DEFINITION_ID, clientAddAccount.PeriodDefinitionId),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, clientAddAccount.CurrencyId),
				new SqlParameter(DatabaseConstants.PAR_BASE_BUDGET, clientAddAccount.BaseBudget),
				new SqlParameter(DatabaseConstants.PAR_FINANCIAL_ENTITY_ID, clientAddAccount.FinancialEntityId),
				new SqlParameter(DatabaseConstants.PAR_HEADER_COLOR,
					ServicesUtils.CreateFrontStyleDataJson(clientAddAccount.HeaderColor)),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_TYPE_ID, (int) clientAddAccount.AccountTypeId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, clientAddAccount.SpendTypeId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_INCLUDE_DATA,
					ServicesUtils.CreateClientAccountIncludeJson(clientAddAccount.AccountIncludes)),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_ID, clientAddAccount.AccountGroupId)
			};

			ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_ADD, parameters);
		}

		public void DeleteAccount(string userId, int accountId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId)
			};

			ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_DELETE, parameters);
		}

		public UserAccountsViewModel GetAccountsByUserId(string userId)
		{
			if (string.IsNullOrEmpty(userId))
				throw new Exception("Invalid value: empty user");
			//var dateParameter = new SqlParameter(DatabaseConstants.PAR_DATE, new DateTime(2016, 7, 20));
			var userParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_MAIN_VIEW_DATA, userParameter);
			return ServicesUtils.CreateMainViewModel(dataSet);
		}

		public Task<IEnumerable<BankAccountPeriodBasicId>> GetBankSummaryAccountsPeriodByUserIdAsync(string userId, DateTime? dateTime)
		{
			var userParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_USER_BANK_SUMMARY_ACCOUNT_PERIOD_LIST, userParameter);
			IEnumerable<BankAccountPeriodBasicId> res;
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				res = Array.Empty<BankAccountPeriodBasicId>();
				return Task.FromResult(res);
			}

			res = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateBankAccountPeriodBasicId);
			return Task.FromResult(res);
		}

		public Task<IEnumerable<AccountViewModel>> GetOrderedAccountViewModelListAsync(IEnumerable<int> accountIds, string userId)
		{
			if (accountIds == null || !accountIds.Any())
			{
				return Task.FromResult((IEnumerable<AccountViewModel>)Array.Empty<AccountViewModel>());
			}

			var idsDataTable = ServicesUtils.CreateIntDataTable(accountIds);
			var userIdParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var accountIdsDataTable = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_IDS, idsDataTable);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_ORDER_LIST, userIdParameter,
				accountIdsDataTable);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				return Task.FromResult((IEnumerable<AccountViewModel>)Array.Empty<AccountViewModel>());
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountViewModel);
			return Task.FromResult(result);
		}

		public IEnumerable<AccountPeriodBasicInfo> GetAccountPeriodBasicInfo(IEnumerable<int> accountPeriodIds)
		{
			if (accountPeriodIds == null)
			{
				throw new ArgumentNullException(nameof(accountPeriodIds));
			}

			if (!accountPeriodIds.Any())
			{
				return new AccountPeriodBasicInfo[] { };
			}

			var idsDataTable = ServicesUtils.CreateIntDataTable(accountPeriodIds);
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_IDS,idsDataTable)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_BASIC_ACCOUNT_PERIOD_LIST, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				return new AccountPeriodBasicInfo[0];
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountPeriodBasicInfo);
			return result;
		}

		public AccountPeriodBasicInfo GetAccountPeriodInfoByAccountIdDateTime(int accountId, DateTime dateTime)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID,accountId),
				new SqlParameter(DatabaseConstants.PAR_DATE_TIME,dateTime)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_BASIC_ACCOUNT_PERIOD_ID_DATE, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				return null;
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountPeriodBasicInfo).First();
			return result;
		}

		public async Task<AccountPeriodBasicInfo> GetAccountPeriodInfoByAccountIdDateTimeAsync(int accountId, DateTime dateTime)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID,accountId),
				new SqlParameter(DatabaseConstants.PAR_DATE_TIME,dateTime)
			};

			var dataSet = await ExecuteStoredProcedureAsync(DatabaseConstants.SP_BASIC_ACCOUNT_PERIOD_ID_DATE, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				return null;
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountPeriodBasicInfo).First();
			return result;
		}

		public IEnumerable<AccountBasicPeriodInfo> GetAccountBasicInfoByAccountId(IEnumerable<int> accountIds)
		{
			if (accountIds == null || !accountIds.Any())
			{
				return null;
			}

			var accountIdsDb = ServicesUtils.CreateIntDataTable(accountIds);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_BASIC_ACCOUNT_BY_ID,
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_IDS, accountIdsDb));
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				return new AccountBasicPeriodInfo[0];
			}

			var result = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountBasicPeriodInfo);
			return result;
		}

		public Task<AccountMainViewModel> GetAccountDetailsViewModelAsync(string userId, int? accountGroupId)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_GROUP_ID, accountGroupId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNT_LIST, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				throw new Exception("Invalid dataSet exception");
			}

			var resultSets = ServicesUtils.CreateGenericList(dataSet.Tables[0],
				ServicesUtils.CreateAccountDetailResultSet);
			var accountDetailsViewModel = CreateAccountDetailsViewModel(resultSets);
			var accountGroupViewModels = ServicesUtils.CreateGenericList(dataSet.Tables[1],
				ServicesUtils.CreateAccountGroupViewModel);
			return Task.FromResult(new AccountMainViewModel
			{
				AccountDetailsViewModels = accountDetailsViewModel,
				AccountGroupViewModels = accountGroupViewModels
			});
		}

		public async Task<IReadOnlyCollection<AccountDetailsPeriodViewModel>> GetAccountDetailsPeriodViewModelAsync(string userId, DateTime dateTime)
		{
			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_DATE, dateTime)
			};

			var dataSet = await ExecuteStoredProcedureAsync(DatabaseConstants.SP_ACCOUNT_W_PERIOD_LIST, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				throw new Exception("Invalid dataSet exception");
			}

			return ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateAccountDetailsPeriodViewModel)
				.ToList();
		}

		#endregion

		#region Private Methods

		#region Create Account View Model

		private static AddAccountViewModel CreateAddAccountViewModel(
			AccountAddViewModelResultSet resultSet)
		{
			if (resultSet == null)
			{
				throw new ArgumentNullException(nameof(resultSet));
			}

			var spendTypeViewModels = CreateSpendTypeViewModel(resultSet.SpendTypeAccountDataResultSetList, resultSet.SpendTypeInfoResultSetList);
			var accountTypeViewModels = CreateAccountTypeViewModel(resultSet.AccountTypeInfoResultSetList);
			var periodTypeViewModels = CreatePeriodTypeViewModel(resultSet.PeriodTypeInfoResultSet);
			var financialEntityViewModels = CreateFinancialEntityViewModel(resultSet.FinancialEntityInfoResultSetList);
			var accountIncludeViewModels = CreateAccountIncludeViewModel(resultSet.AccountIncludeAccountInfoResultSetList);
			var currencyViewModels = CreateCurrencyViewModel(resultSet.CurrencyDataResultSetList);
			var groupAccountViewModels = CreateAccountGroupViewModel(resultSet.AccountGroupResultSetList);
			return new AddAccountViewModel
			{
				AccountTypeViewModels = accountTypeViewModels,
				PeriodTypeViewModels = periodTypeViewModels,
				FinancialEntityViewModels = financialEntityViewModels,
				SpendTypeViewModels = spendTypeViewModels,
				AccountIncludeViewModels = accountIncludeViewModels,
				CurrencyViewModels = currencyViewModels,
				AccountName = "New account",
				AccountStyle = new FrontStyleData(),
				BaseBudget = 0,
				AccountGroupViewModels = groupAccountViewModels
			};
		}

		private static IEnumerable<AccountIncludeViewModel> CreateAccountIncludeViewModel(
			IEnumerable<AccountIncludeInfoResultSet> accountIncludeInfoResultSets)
		{
			if (accountIncludeInfoResultSets == null)
			{
				throw new ArgumentNullException(nameof(accountIncludeInfoResultSets));
			}

			//
			var accountIncludeViewModelList = new List<AccountIncludeViewModel>();
			foreach (var resultSet in accountIncludeInfoResultSets)
			{
				var model = accountIncludeViewModelList.FirstOrDefault(i => i.AccountId == resultSet.AccountId);
				if (model == null)
				{
					model = new AccountIncludeViewModel
					{
						AccountId = resultSet.AccountId,
						AccountName = resultSet.AccountName,
						Amount = null,
						MethodIds = new List<MethodId>()
					};

					accountIncludeViewModelList.Add(model);
				}

				((List<MethodId>)model.MethodIds).Add(new MethodId
				{
					Id = resultSet.CurrencyConverterMethodId,
					Name = resultSet.CurrencyConverterMethodName,
					IsDefault = resultSet.IsDefault,
					IsSelected = resultSet.IsSelected
				});
			}

			return accountIncludeViewModelList;
		}

		private static IEnumerable<AccountDetailsInfoViewModel> GetAccountDetailsViewModel(
			AccountEditViewModelResultSet resultSet)
		{
			if (resultSet == null)
			{
				throw new ArgumentNullException(nameof(resultSet));
			}

			return resultSet.AccountDetailResultSetList.Select(i => CreateAccountDetailsViewModel(i, resultSet));
		}

		private static AccountDetailsInfoViewModel CreateAccountDetailsViewModel(
			AccountDetailResultSet accountDetailResultSet, AccountEditViewModelResultSet resultSet)
		{
			if (resultSet == null)
			{
				throw new ArgumentNullException(nameof(resultSet));
			}

			if (accountDetailResultSet == null)
			{
				throw new ArgumentNullException(nameof(accountDetailResultSet));
			}

			var frontStyleData = ServicesUtils.CreateFrontStyleData(accountDetailResultSet.AccountHeaderColor);
			var spendTypeViewModels = CreateSpendTypeViewModel(
				resultSet.SpendTypeAccountDataResultSetList.Where(i => i.AccountId == accountDetailResultSet.AccountId)
					.Select(j => j.SpendTypeId),
				resultSet.SpendTypeInfoResultSetList,
				accountDetailResultSet.SpendTypeId);
			var accountTypeViewModels = CreateAccountTypeViewModel(resultSet.AccountTypeInfoResultSetList,
				accountDetailResultSet.AccountTypeId);
			var periodTypeViewModels = CreatePeriodTypeViewModel(resultSet.PeriodTypeInfoResultSet,
				accountDetailResultSet.PeriodDefinitionId);
			var financialEntityViewModels = CreateFinancialEntityViewModel(resultSet.FinancialEntityInfoResultSetList,
				accountDetailResultSet.FinancialEntityId);
			var accountIncludeViewModels =
				CreateAccountIncludeViewModel(
					resultSet.AccountIncludeAccountInfoResultSetList.Where(i => i.AccountId == accountDetailResultSet.AccountId));
			var currencyViewModels = CreateCurrencyViewModel(resultSet.CurrencyDataResultSetList,
				accountDetailResultSet.CurrencyId);
			var accountGroupViewModels = CreateAccountGroupViewModel(resultSet.AccountGroupResultSetList,
				accountDetailResultSet.AccountGroupId);
			return new AccountDetailsInfoViewModel
			{
				AccountId = accountDetailResultSet.AccountId,
				AccountName = accountDetailResultSet.AccountName,
				AccountPosition = accountDetailResultSet.AccountPosition,
				AccountStyle = frontStyleData,
				BaseBudget = accountDetailResultSet.BaseBudget,
				AccountTypeViewModels = accountTypeViewModels,
				PeriodTypeViewModels = periodTypeViewModels,
				FinancialEntityViewModels = financialEntityViewModels,
				SpendTypeViewModels = spendTypeViewModels,
				AccountIncludeViewModels = accountIncludeViewModels,
				CurrencyViewModels = currencyViewModels,
				AccountGroupViewModels = accountGroupViewModels
			};
		}

		private static IEnumerable<AccountGroupViewModel> CreateAccountGroupViewModel(
			IEnumerable<AccountGroupResultSet> accountGroupResultSets, int? accountGroupId = null)
		{
			return accountGroupResultSets.Select(i => CreateAccountGroupViewModel(i, i.AccountGroupId == accountGroupId));
		}

		private static AccountGroupViewModel CreateAccountGroupViewModel(
			AccountGroupResultSet accountGroupResultSet, bool isSelected)
		{
			return new AccountGroupViewModel
			{
				AccountGroupId = accountGroupResultSet.AccountGroupId,
				AccountGroupName = accountGroupResultSet.AccountGroupName,
				IsSelected = isSelected
			};
		}

		private static IEnumerable<CurrencyViewModel> CreateCurrencyViewModel(
			IEnumerable<CurrencyDataResultSet> currencyDataResultSets, int? currencyId = null)
		{
			var result = currencyDataResultSets.Select(i => CreateCurrencyViewModel(i, i.CurrencyId == currencyId));
			return result;
		}

		private static CurrencyViewModel CreateCurrencyViewModel(CurrencyDataResultSet currencyDataResultSet, bool isDefault)
		{
			return new CurrencyViewModel
			{
				CurrencyId = currencyDataResultSet.CurrencyId,
				CurrencyName = currencyDataResultSet.Name,
				MethodIds = new List<MethodId>(),
				Symbol = currencyDataResultSet.Symbol,
				Isdefault = isDefault
			};
		}

		private static IEnumerable<AccountIncludeViewModel> CreateAccountIncludeViewModel(
			IEnumerable<AccountIncludeAccountInfoResultSet> accountIncludeAccountInfoResultSets)
		{
			var validResultSet = accountIncludeAccountInfoResultSets;
			var modelList = new List<AccountIncludeViewModel>();
			foreach (var resultSet in validResultSet)
			{
				var accountInclude = modelList.FirstOrDefault(i => i.AccountId == resultSet.AccountIncludeId);
				if (accountInclude == null)
				{
					accountInclude = new AccountIncludeViewModel
					{
						AccountId = resultSet.AccountIncludeId,
						AccountName = resultSet.AccountIncludeName,
						IsDefault = false,
						MethodIds = new List<MethodId>(),
						Amount = null,
						IsSelected = resultSet.IsSelected
					};
					modelList.Add(accountInclude);
				}

				var methodId = new MethodId
				{
					Id = resultSet.CurrencyConverterMethodId,
					Name = resultSet.CurrencyConverterMethodName,
					IsDefault = resultSet.IsDefault,
					IsSelected = resultSet.IsSelected
				};

				((List<MethodId>)accountInclude.MethodIds).Add(methodId);
				accountInclude.IsSelected = resultSet.IsSelected || accountInclude.IsSelected;
			}

			return modelList;
		}

		private static IEnumerable<FinancialEntityViewModel> CreateFinancialEntityViewModel(
			IEnumerable<FinancialEntityInfoResultSet> financialEntityInfoResultSets, int? financialEntityId = null)
		{
			return
				financialEntityInfoResultSets.Select(
					i => CreateFinancialEntityViewModel(i, i.FinancialEntityId == financialEntityId));
		}

		private static FinancialEntityViewModel CreateFinancialEntityViewModel(
			FinancialEntityInfoResultSet financialEntityInfoResultSet, bool isDefault)
		{
			return new FinancialEntityViewModel
			{
				FinancialEntityId = financialEntityInfoResultSet.FinancialEntityId,
				FinancialEntityName = financialEntityInfoResultSet.FinancialEntityName,
				IsDefault = isDefault
			};
		}

		private static IEnumerable<PeriodTypeViewModel> CreatePeriodTypeViewModel(
			IEnumerable<PeriodTypeInfoResultSet> periodTypeInfoResultSets, int? periodDefinitionId = null)
		{
			return
				periodTypeInfoResultSets.Select(
					i =>
						CreatePeriodTypeViewModel(i,
							(i.PeriodDefinitionId == periodDefinitionId)));
		}

		private static PeriodTypeViewModel CreatePeriodTypeViewModel(PeriodTypeInfoResultSet periodTypeInfoResultSet, bool isDefault)
		{
			return new PeriodTypeViewModel
			{
				PeriodDefinitionId = periodTypeInfoResultSet.PeriodDefinitionId,
				CuttingDate = periodTypeInfoResultSet.CuttingDate,
				IsDefault = isDefault,
				PeriodTypeId = periodTypeInfoResultSet.PeriodTypeId,
				PeriodTypeName = periodTypeInfoResultSet.PeriodTypeName,
				Repetition = periodTypeInfoResultSet.Repetition
			};
		}

		private static IEnumerable<AccountTypeViewModel> CreateAccountTypeViewModel(
			IEnumerable<AccountTypeInfoResultSet> accountTypeInfoResultSets, int? accountTypeId = null)
		{
			return accountTypeInfoResultSets.Select(i => CreateAccountTypeViewModel(i, i.AccountTypeId == accountTypeId));
		}

		private static AccountTypeViewModel CreateAccountTypeViewModel(
			AccountTypeInfoResultSet accountTypeInfoResultSet, bool isDefault)
		{
			return new AccountTypeViewModel
			{
				AccountTypeId = accountTypeInfoResultSet.AccountTypeId,
				AccountTypeName = accountTypeInfoResultSet.AccountTypeName,
				IsDefault = isDefault
			};
		}

		private static IEnumerable<SpendTypeViewModel> CreateSpendTypeViewModel(
			IEnumerable<int> spendTypeIds,
			IEnumerable<SpendTypeInfoResultSet> spendTypeInfoResultSetList, int? defaultSpendTypeId = null)
		{
			return
				spendTypeInfoResultSetList.Where(
					i =>
						spendTypeIds.Any(
							j => j == i.SpendTypeId))
					.Select(k => CreateSpendTypeViewModel(k, k.SpendTypeId == defaultSpendTypeId));

		}

		private static SpendTypeViewModel CreateSpendTypeViewModel(
			SpendTypeInfoResultSet spendTypeInfoResultSet, bool isDefault)
		{
			return new SpendTypeViewModel
			{
				SpendTypeId = spendTypeInfoResultSet.SpendTypeId,
				SpendTypeName = spendTypeInfoResultSet.SpendTypeName,
				Description = spendTypeInfoResultSet.SpendTypeDescription,
				IsDefault = isDefault
			};
		}

		#endregion

		private static IEnumerable<AccountDetailsViewModel> CreateAccountDetailsViewModel(
			IEnumerable<AccountDetailResultSet> accountDetailResultSets)
		{
			if (accountDetailResultSets == null)
			{
				throw new ArgumentNullException(nameof(accountDetailResultSets));
			}

			return accountDetailResultSets.Select(i => new AccountDetailsViewModel
			{
				AccountId = i.AccountId,
				AccountName = i.AccountName,
				AccountPosition = i.AccountPosition,
				AccountStyle = ServicesUtils.CreateFrontStyleData(i.AccountHeaderColor),
				AccountGroupId = i.AccountGroupId,
				BaseBudget = i.BaseBudget
			});
		}

		private static DataTable CreateClientAddSpendAccountIncludeUpdateDataTable(
			IEnumerable<ClientAddSpendAccountIncludeUpdate> listUpdates)
		{
			if (listUpdates == null || !listUpdates.Any())
				throw new ArgumentNullException(nameof(listUpdates));
			var dataTable = new DataTable();
			dataTable.Columns.Add("AccountId");
			dataTable.Columns.Add("AmountCurrencyId");
			dataTable.Columns.Add("AccountIncludeId");
			dataTable.Columns.Add("CurrencyConverterMethodId");
			foreach (var listUpdate in listUpdates)
			{
				if (listUpdate.AccountId < 1 || listUpdate.AmountCurrencyId < 1 || listUpdate.AccountIncludeId < 1 ||
				    listUpdate.CurrencyConverterMethodId < 1)
				{
					throw new Exception("Ids cannot be null");
				}
				dataTable.Rows.Add(listUpdate.AccountId, listUpdate.AmountCurrencyId, listUpdate.AccountIncludeId,
					listUpdate.CurrencyConverterMethodId);
			}
			return dataTable;
		}

		private static IEnumerable<SupportedAccountIncludeViewModel> CreateSupportedAccountIncludeViewModel(
			SupportedAccountIncludeViewModelDb supportedAccountIncludeViewModelDb)
		{
			if (supportedAccountIncludeViewModelDb == null)
				return new List<SupportedAccountIncludeViewModel>();
			return
				supportedAccountIncludeViewModelDb.AccountIdList.Select(
					i =>
						CreateSupportedAccountIncludeViewModel(i,
							supportedAccountIncludeViewModelDb.SupportedAccountData,
							supportedAccountIncludeViewModelDb.CurrencyConverterMethodDataList,
							supportedAccountIncludeViewModelDb.AccountIncludeMethodList));
		}

		private static SupportedAccountIncludeViewModel CreateSupportedAccountIncludeViewModel(int accountId,
			IEnumerable<SupportedAccountDataResultSet> supportedAccountDataResultSets,
			IEnumerable<CurrencyConverterMethodDataResultSet> currencyConverterMethodDataResultSets,
			IEnumerable<AccountIncludeMethodResultSet> accountIncludeMethodResultSets)

		{
			if (accountId == 0)
				throw new ArgumentException(@"value cannot be null", nameof(accountId));
			return new SupportedAccountIncludeViewModel
			{
				AccountId = accountId,
				AccountIncludeViewModels =
					CreateAccountIncludeViewModel(accountId, supportedAccountDataResultSets,
						currencyConverterMethodDataResultSets, accountIncludeMethodResultSets)
			};
		}


		private static IEnumerable<AccountIncludeViewModel> CreateAccountIncludeViewModel(int accountId,
																			IEnumerable<SupportedAccountDataResultSet>
																				supportedAccountDataResultSets,
																			IEnumerable
																				<CurrencyConverterMethodDataResultSet>
																				currencyConverterMethodDataResultSets,
																			IEnumerable<AccountIncludeMethodResultSet>
																				accountIncludeMethodResultSets)
		{
			var accountIncludeIds = new List<int>();
			accountIncludeIds.AddRange(accountIncludeMethodResultSets.Where(item => item.AccountId == accountId)
																	 .Select(i => i.AccountIncludeId)
																	 .Where(j => accountIncludeIds.All(k => k != j)));
			var supportedAccountDataList =
				supportedAccountDataResultSets.Where(item => accountIncludeIds.Any(i => i == item.AccountId));
			var accountIncludeViewModels =
				supportedAccountDataList.Select(
					item =>
					CreateAccountIncludeViewModel(accountId, item, currencyConverterMethodDataResultSets,
												 accountIncludeMethodResultSets));
			return accountIncludeViewModels;
		}

		private static AccountIncludeViewModel CreateAccountIncludeViewModel(int accountId,
																			SupportedAccountDataResultSet
																				supportedAccountDataResultSet,
																			IEnumerable<CurrencyConverterMethodDataResultSet>
																				currencyConverterMethodDataResultSets,
																			IEnumerable<AccountIncludeMethodResultSet>
																				accountIncludeMethodResultSets)
		{
			var methodIds = CreateMethodId(accountId, supportedAccountDataResultSet.AccountId,
										   currencyConverterMethodDataResultSets, accountIncludeMethodResultSets);
			var accountIncludeViewModel = new AccountIncludeViewModel
			{
				AccountId = supportedAccountDataResultSet.AccountId,
				AccountName = supportedAccountDataResultSet.Name,
				IsCurrentSelection = accountIncludeMethodResultSets.Any(
					item =>
						item.AccountId == accountId &&
						item.AccountIncludeId == supportedAccountDataResultSet.AccountId && item.IsCurrentSelection),
				IsDefault =
						accountIncludeMethodResultSets.Any(
							item =>
							item.AccountId == accountId &&
							item.AccountIncludeId == supportedAccountDataResultSet.AccountId && item.IsDefault),
				MethodIds = methodIds
			};
			return accountIncludeViewModel;

		}

		private static IEnumerable<MethodId> CreateMethodId(int accountId, int accountIncludeId,
															IEnumerable<CurrencyConverterMethodDataResultSet>
																currencyConverterMethodDataResultSets,
															IEnumerable<AccountIncludeMethodResultSet>
																accountIncludeMethodResultSets)
		{
			var ids =
				accountIncludeMethodResultSets.Where(
					item => item.AccountId == accountId && item.AccountIncludeId == accountIncludeId)
											  .Select(item => item.CurrencyConverterMethodId);
			var methodIds =
				currencyConverterMethodDataResultSets.Where(item => ids.Any(i => i == item.CurrencyConverterMethodId))
					.Select(ccm => CreateMethodId(ccm));
			var methodIdsArray = methodIds.ToArray();
			foreach (var methodId in methodIdsArray)
			{
				var accountIncludeMethodResultSet =
					accountIncludeMethodResultSets.FirstOrDefault(
						item =>
						item.AccountId == accountId && item.AccountIncludeId == accountIncludeId &&
						methodId.Id == item.CurrencyConverterMethodId);
				if (accountIncludeMethodResultSet != null)
					methodId.IsDefault = accountIncludeMethodResultSet.IsDefault;
			}
			return methodIdsArray;
		}

		private static MethodId CreateMethodId(CurrencyConverterMethodDataResultSet currencyConverterMethodDataResultSet,
			SpendModelResultSet spendModelResultSet = null, AccountCurrencyResultSet accountCurrencyResultSet = null)
		{
			if (currencyConverterMethodDataResultSet == null)
				throw new ArgumentNullException(nameof(currencyConverterMethodDataResultSet));

			return new MethodId
			{
				Id = currencyConverterMethodDataResultSet.CurrencyConverterMethodId,
				Name = currencyConverterMethodDataResultSet.CurrencyConverterMethodName,
				IsDefault = currencyConverterMethodDataResultSet.IsDefault ||
							(spendModelResultSet?.CurrencyConverterMethodId ==
							 currencyConverterMethodDataResultSet.CurrencyConverterMethodId) ||
							(accountCurrencyResultSet != null && accountCurrencyResultSet.IsSuggested)
			};
		}

		public Task<AccountNotes> UpdateNotes(AccountNotes accountNotes, int accountId)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Internal classes

		internal class SupportedAccountIncludeViewModelDb
		{
			public IEnumerable<int> AccountIdList { get; set; }
			public IEnumerable<SupportedAccountDataResultSet> SupportedAccountData { get; set; }
			public IEnumerable<CurrencyConverterMethodDataResultSet> CurrencyConverterMethodDataList { get; set; }
			public IEnumerable<AccountIncludeMethodResultSet> AccountIncludeMethodList { get; set; }
		}

		internal class EditSpendViewModelDb : AddSpendViewModelDb
		{
			public IEnumerable<DateRangeResultSet> DateRangeList { get; set; }
			public IEnumerable<SpendModelResultSet> SpendViewModelList { get; set; }
			public IEnumerable<SpendIncludeModelResultSet> SpendIncludeModelList { get; set; }
		}

		internal class AddSpendViewModelDb
		{
			public IEnumerable<CurrencyConverterMethodDataResultSet> CurrencyConverterMethodDataList { get; set; }
			public IEnumerable<SupportedAccountDataResultSet> SupportedAccountData { get; set; }
			public IEnumerable<CurrencyDataResultSet> CurrencyDataList { get; set; }
			public IEnumerable<SpendTypeInfoResultSet> SpendTypeDataList { get; set; }
			public IEnumerable<AccountIncludeDataResultSet> AccountIncludeDataList { get; set; }
			public IEnumerable<AccountDataResultSet> AccountDataList { get; set; }
			public IEnumerable<AccountIncludeMethodResultSet> AccountIncludeMethodList { get; set; }
			public IEnumerable<AccountCurrencyResultSet> AccountCurrencyList { get; set; }
			public IEnumerable<SpendTypeDefaultResultSet> SpendTypeDefaultList { get; set; }
		}

		#endregion
	}

}
