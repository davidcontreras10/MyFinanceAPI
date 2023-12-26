using DataAccess;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Models;
using MyFinanceBackend.Services;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Utilities;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public class SpendsRepository : SqlServerBaseService, ISpendsRepository
	{
		private readonly ITrxExchangeService _trxExchangeService;

		public SpendsRepository(IConnectionConfig connectionConfig, ITrxExchangeService trxExchangeService)
			: base(connectionConfig)
		{
			_trxExchangeService = trxExchangeService;
		}

		#region Public Methods

		public Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModelAsync(IEnumerable<int> accountPeriodIds, string userId)
		{
			var accountPeriodIdsString = ServicesUtils.CreateStringCharSeparated(accountPeriodIds);
			var accountPeriodIdsParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_IDS, accountPeriodIdsString);
			var userIdParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ADD_SPEND_VIEW_MODEL_LIST,
				accountPeriodIdsParameter, userIdParameter);
			var addSpendViewModel = ServicesUtils.CreateAddSpendViewModelDb(dataSet);
			var result = CreateAddSpendViewModelList(addSpendViewModel);
			return Task.FromResult(result);
		}

		public Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModelAsync(int accountPeriodId, int spendId, string userId)
		{
			var userIdParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var accountPeriodIdsParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_ID, accountPeriodId);
			var spendIdParameter = new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_EDIT_SPEND_VIEW_MODEL_LIST,
				accountPeriodIdsParameter, userIdParameter, spendIdParameter);
			var resultSetModel = ServicesUtils.CreateEditSpendViewModelDb(dataSet);
			var result = CreateEditSpendViewModelList(resultSetModel);
			return Task.FromResult(result);
		}

		public IEnumerable<SupportedAccountIncludeViewModel> GetSupportedAccountIncludeViewModel(
			IEnumerable<ClientAddSpendAccountIncludeUpdate> listUpdates, string userId)
		{
			var dataTable = CreateClientAddSpendAccountIncludeUpdateDataTable(listUpdates);
			var dataTableParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_INCLUDE_UPDATE_TABLE, dataTable);
			var userIdParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ADD_SPEND_ACCOUNT_INCLUDE_LIST, userIdParameter,
				dataTableParameter);
			var supportedAccountIncludeViewModelDb = ServicesUtils.CreateSupportedAccountIncludeViewModelDb(dataSet);
			var result = CreateSupportedAccountIncludeViewModel(supportedAccountIncludeViewModelDb);
			return result;
		}

		public Task<AccountFinanceViewModel> GetAccountFinanceViewModelAsync(int accountPeriodId, string userId)
		{
			if (accountPeriodId == 0)
				throw new ArgumentException(@"Value cannot be zero", nameof(accountPeriodId));
			var parameters = new List<SqlParameter>
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_ID, accountPeriodId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_TRANSFER_ACCOUNT_INFO, parameters);
			var res = dataSet.Tables.Count > 0 ? CreateAccountFinanceViewModel(dataSet.Tables[0]) : null;
			return Task.FromResult(res);
		}

		public Task<IEnumerable<AccountFinanceViewModel>> GetAccountFinanceViewModelAsync(IEnumerable<ClientAccountFinanceViewModel> requestItems, string userId)
		{
			requestItems = RemoveDuplicated(requestItems.ToList());
			var accountPeriodIds = CreateAccountPeriodOptionParametersTable(requestItems);
			var accountPeriodIdsParameter = new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_TABLE, accountPeriodIds);
			var userIdParameter = new SqlParameter(DatabaseConstants.PAR_USER_ID, userId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_FINANCE_SPEND_BY_ACCOUNT_LIST,
				accountPeriodIdsParameter, userIdParameter);
			if (dataSet == null || dataSet.Tables.Count == 0)
			{
				var res = (IEnumerable<AccountFinanceViewModel>)Array.Empty<AccountFinanceViewModel>();
				return Task.FromResult(res);
			}
			var accountFinanceResultSet = ServicesUtils.CreateAccountFinanceResultSet(dataSet.Tables[0]);
			var accountFinanceViewModelList = CreateAccountFinanceViewModel(accountFinanceResultSet);
			return Task.FromResult(accountFinanceViewModelList);
		}

		public Task<SpendActionAttributes> GetSpendAttributesAsync(int spendId)
		{
			var parameter = new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_ATTRIBUTES_LIST, parameter);
			if (dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
			{
				return null;
			}

			var result = ServicesUtils.CreateSpendAttributes(dataSet.Tables[0].Rows[0]);
			return Task.FromResult(result);
		}
		
		public Task AddSpendDependencyAsync(int spendId, int dependencySpendId)
		{
			var parameters = new[]
{
				new SqlParameter(DatabaseConstants.PAR_SPEN_DEPENDENCY_ID, dependencySpendId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId)
			};

			ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_DEPENDENCY_ADD, parameters);
			return Task.CompletedTask;
		}

		public async Task<ClientAddSpendModel> CreateClientAddSpendModelAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId)
		{
			var accounts = GetAccountPeriodBasicInfo(new[] { accountPeriodId });
			var accountId = accounts.First().AccountId;
			var accountCurrencyInfo = await GetAccountMethodConversionInfoAsync(accountId, null,
				clientBasicAddSpend.UserId, clientBasicAddSpend.CurrencyId);
			var originalAccountData = accountCurrencyInfo.FirstOrDefault(a => a.AccountId == accountId);
			var includeAccountData = accountCurrencyInfo.Where(a => a.AccountId != accountId);
			var clientAddSpendModel = new ClientAddSpendModel
			{
				Amount = clientBasicAddSpend.Amount,
				Description = clientBasicAddSpend.Description,
				CurrencyId = clientBasicAddSpend.CurrencyId,
				SpendTypeId = clientBasicAddSpend.SpendTypeId,
				SpendDate = clientBasicAddSpend.SpendDate,
				UserId = clientBasicAddSpend.UserId,
				OriginalAccountData = originalAccountData,
				IncludedAccounts = includeAccountData,
				IsPending = clientBasicAddSpend.IsPending,
				AmountTypeId = clientBasicAddSpend.AmountTypeId,
				AmountType = clientBasicAddSpend.AmountType
			};

			return clientAddSpendModel;
		}

		public Task<IEnumerable<SpendItemModified>> DeleteSpendAsync(string userId, int spendId)
		{
			if (string.IsNullOrEmpty(userId) || spendId == 0)
			{
				throw new Exception("Invalid parameters");
			}

			var emptyIntArray = new int[0];
			var emptyIntArrayParameter = ServicesUtils.CreateIntDataTable(emptyIntArray);
			var parameters = new List<SqlParameter>
				{
					new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
					new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId),
					new SqlParameter(DatabaseConstants.PAR_IGNORE_ID_LIST, emptyIntArrayParameter)
				};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_DELETE, parameters);
			var res = ServicesUtils.CreateSpendAccountAffected(dataSet);
			return Task.FromResult(res);
		}

		public Task<IEnumerable<SpendItemModified>> EditSpendAsync(ClientEditSpendModel model)
		{
			if (model == null || model.SpendId == 0 || string.IsNullOrEmpty(model.UserId) || !model.ModifyList.Any() ||
				model.ModifyList.Any(i => i == 0) || model.ModifyList.Any(i => !((int)i).TryParseEnum<ClientEditSpendModel.Field>(out _)))
				throw new Exception("Invalid parameters");
			SpendsDataHelper.SetAmountType(model, model.ModifyList.Any(i => i == ClientEditSpendModel.Field.AmountType));
			var modifyList = ServicesUtils.CreateStringCharSeparated(model.ModifyList.Select(ml => (int)ml));
			//var databaseValues = CreateAddSpendDbValues(model);
			//var accountsTable = ServicesUtils.ClientAddSpendAccountDataTable(databaseValues.IncludedAccounts);
			var accountsTable = ServicesUtils.ClientAddSpendAccountDataTable(null, true);
			var parameters = new List<SqlParameter>
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, model.UserId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_ID, model.SpendId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNTS_TABLE, accountsTable),
				new SqlParameter(DatabaseConstants.PAR_MODIFY_LIST, modifyList),
                //new SqlParameter(DatabaseConstants.PAR_SPEND_DATE, model.SpendDate),
                new SqlParameter(DatabaseConstants.PAR_AMOUNT, model.Amount),
				new SqlParameter(DatabaseConstants.PAR_DESCRIPTION, model.Description),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, model.SpendTypeId),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, model.CurrencyId),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_TYPE_NAME, model.AmountType)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_EDIT, parameters);
			var result = ServicesUtils.CreateSpendAccountAffected(dataSet);
			return Task.FromResult(result);
		}

		public Task<IEnumerable<AccountCurrencyPair>> GetAccountsCurrencyAsync(IEnumerable<int> accountIdsArray)
		{
			if (accountIdsArray == null || !accountIdsArray.Any())
				throw new ArgumentException(@"Cannot be null or empty", nameof(accountIdsArray));
			var accountIds = accountIdsArray.Aggregate("", (current, i) => current + ("," + i));
			if (accountIds.Any() && accountIds[0] == ',')
				accountIds = accountIds.Remove(0, 1);
			var parameters = new List<SqlParameter>
				{
					new SqlParameter(DatabaseConstants.PAR_ACCOUNT_IDS, accountIds)
				};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ACCOUNTS_CURRENCIES_LIST, parameters);
			if (dataSet == null || dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				throw new Exception("Empty result set for " + DatabaseConstants.SP_ACCOUNTS_CURRENCIES_LIST);
			return Task.FromResult(dataSet.Tables[0].Rows.Cast<DataRow>().Select(ServicesUtils.CreateAccountCurrencyPair));
		}

		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel)
		{
			if (clientAddSpendModel == null)
				throw new ArgumentNullException(nameof(clientAddSpendModel));
			if (clientAddSpendModel.OriginalAccountData == null)
				throw new ArgumentException(@"OriginalAccountData is null", nameof(clientAddSpendModel));

			ValidateSpendCurrencyConvertibleValues(clientAddSpendModel);
			SpendsDataHelper.SetAmountType(clientAddSpendModel, false);
			var databaseValues = await CreateAddSpendDbValuesAsync(clientAddSpendModel);
			var parameters = new List<SqlParameter>
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, databaseValues.UserId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_TYPE_ID, databaseValues.SpendTypeId),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT, databaseValues.Amount),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_NUMERATOR, databaseValues.AmountNumerator),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_DENOMINATOR, databaseValues.AmountDenominator),
				new SqlParameter(DatabaseConstants.PAR_SPEND_DATE, databaseValues.SpendDate),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, databaseValues.CurrencyId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_DESCRIPTION, clientAddSpendModel.Description),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNTS_TABLE,
					ServicesUtils.ClientAddSpendAccountDataTable(databaseValues.IncludedAccounts)),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_TYPE_NAME, clientAddSpendModel.AmountType),
				new SqlParameter(DatabaseConstants.PAR_IS_PENDING,clientAddSpendModel.IsPending)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_ADD_BY_ACCOUNT, parameters);
			return ServicesUtils.CreateSpendAccountAffected(dataSet);
		}

		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId)
		{
			var clientAddSpendModel = await CreateClientAddSpendModelAsync(clientBasicAddSpend, accountPeriodId);
			var result = await AddSpendAsync(clientAddSpendModel);
			return result;
		}

		public async Task<IEnumerable<SpendItemModified>> EditSpendAsync(FinanceSpend financeSpend)
		{
			ValidateSpendCurrencyConvertibleValues(financeSpend);
			if (financeSpend.OriginalAccountData == null)
				throw new ArgumentException(@"OriginalAccountData is null", nameof(financeSpend));

			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(financeSpend);
			var accountCurrencyPairList = await GetAccountsCurrencyAsync(accountIds);
			var accountIncludes = await _trxExchangeService.ConvertTrxCurrencyAsync(financeSpend, accountCurrencyPairList.ToList());
			var parameters = new List<SqlParameter>
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, financeSpend.UserId),
				new SqlParameter(DatabaseConstants.PAR_SPEND_ID, financeSpend.SpendId),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT, financeSpend.Amount),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_NUMERATOR, financeSpend.AmountNumerator),
				new SqlParameter(DatabaseConstants.PAR_AMOUNT_DENOMINATOR, financeSpend.AmountDenominator),
				new SqlParameter(DatabaseConstants.PAR_SPEND_DATE, financeSpend.SpendDate),
				new SqlParameter(DatabaseConstants.PAR_SPEND_SET_PAYMENT_DATE, financeSpend.SetPaymentDate),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, financeSpend.CurrencyId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNTS_TABLE,
					ServicesUtils.ClientAddSpendAccountDataTable(accountIncludes)),
				new SqlParameter(DatabaseConstants.PAR_IS_PENDING, financeSpend.IsPending)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_EDIT_BY_ACCOUNT, parameters);
			return ServicesUtils.CreateSpendAccountAffected(dataSet);
		}

		public Task<IEnumerable<SavedSpend>> GetSavedSpendsAsync(int spendId)
		{
			var spendIdParameter = new SqlParameter(DatabaseConstants.PAR_SPEND_ID, spendId);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPENDS_SAVED_LIST, spendIdParameter);
			var resultSets = ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateSavedSpend);
			var saveSpends = CreateSavedSpendList(resultSets);
			return Task.FromResult(saveSpends);
		}

		public Task<IEnumerable<ClientAddSpendAccount>> GetAccountMethodConversionInfoAsync(int? accountId, int? accountPeriodId,
			string userId, int currencyId)
		{

			if (string.IsNullOrEmpty(userId))
			{
				throw new ArgumentNullException(nameof(userId));
			}

			if (currencyId == 0)
			{
				throw new ArgumentNullException(nameof(currencyId));
			}

			var parameters = new List<SqlParameter>
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_CURRENCY_ID, currencyId),
			};

			if ((accountId == null || accountId == 0) && (accountPeriodId == null || accountPeriodId == 0))
			{
				throw new AggregateException(new ArgumentException(@"Both parameters cannot be empty",
					nameof(accountId)));
			}

			if ((accountId != null && accountId != 0) && (accountPeriodId != null && accountPeriodId != 0))
			{
				throw new AggregateException(new ArgumentException(@"Only one parameters can be specified",
					nameof(accountId)));
			}

			if (accountId != null && accountId != 0)
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId));
			}
			else
			{
				parameters.Add(new SqlParameter(DatabaseConstants.PAR_ACCOUNT_PERIOD_ID, accountPeriodId));
			}


			var resultSet = ExecuteStoredProcedure(DatabaseConstants.SP_TRANSFER_ACCOUNT_DEFAULT_CURRENCY_VALUES_LIST,
				parameters);
			if (resultSet == null || resultSet.Tables.Count == 0)
				return null;

			var list = ServicesUtils.CreateGenericList(resultSet.Tables[0],
				ServicesUtils.CreateClientAddSpendAccount);
			return Task.FromResult(list);
		}

		public Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrenciesAsync(int accountId, string userId)
		{
			if (accountId == 0)
				throw new ArgumentException(@"Value cannot be zero", nameof(accountId));

			if (string.IsNullOrEmpty(userId))
				throw new ArgumentNullException(nameof(userId));

			var parameters = new[]
			{
				new SqlParameter(DatabaseConstants.PAR_USER_ID, userId),
				new SqlParameter(DatabaseConstants.PAR_ACCOUNT_ID, accountId)
			};

			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_SPEND_POSSIBLE_CURRENCIES, parameters);
			if (dataSet == null || dataSet.Tables.Count < 0)
			{
				IEnumerable<CurrencyViewModel> emptyResult = Array.Empty<CurrencyViewModel>();
				return Task.FromResult(emptyResult);
			}

			return Task.FromResult(ServicesUtils.CreateGenericList(dataSet.Tables[0], ServicesUtils.CreateCurrencyViewModel));
		}

		private IEnumerable<AccountPeriodBasicInfo> GetAccountPeriodBasicInfo(IEnumerable<int> accountPeriodIds)
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

		void ITransactional.BeginTransaction()
		{
			base.BeginTransaction();
		}

		void ITransactional.RollbackTransaction()
		{
			base.RollbackTransaction();
		}

		void ITransactional.Commit()
		{
			base.Commit();
		}

		#endregion

		#region Private Methods

		private static ClientAccountFinanceViewModel CreateBankAccountClientAccountFinanceRequest(int accountPeriodId)
		{
			return new ClientAccountFinanceViewModel
			{
				AccountPeriodId = accountPeriodId,
				AmountTypeId = 0,
				LoanSpends = true,
				PendingSpends = false
			};
		}

		private static IEnumerable<ClientAccountFinanceViewModel> RemoveDuplicated(ICollection<ClientAccountFinanceViewModel> list)
		{
			if (list == null)
			{
				return new ClientAccountFinanceViewModel[0];
			}

			var repeatedList = new List<ClientAccountFinanceViewModel>();
			foreach (var viewModel in list)
			{
				if (list.Count(item => item.AccountPeriodId == viewModel.AccountPeriodId) > 1)
				{
					if (repeatedList.All(item => item.AccountPeriodId != viewModel.AccountPeriodId))
					{
						repeatedList.Add(viewModel);
					}
				}
			}

			foreach (var viewModel in repeatedList)
			{
				list.Remove(viewModel);
			}

			return list;
		}

		private static DataTable CreateAccountPeriodOptionParametersTable(IEnumerable<ClientAccountFinanceViewModel> requestItems)
		{
			var dataTable = new DataTable();
			dataTable.Columns.Add("AccountPeriodId");
			dataTable.Columns.Add("PendingSpends");
			dataTable.Columns.Add("LoanSpends");
			dataTable.Columns.Add("AmountTypeId");
			if (requestItems == null)
			{
				return dataTable;
			}

			foreach (var value in requestItems)
			{
				dataTable.Rows.Add(value.AccountPeriodId, value.PendingSpends, value.LoanSpends, value.AmountTypeId);
			}

			return dataTable;
		}


		private static IEnumerable<EditSpendViewModel> CreateEditSpendViewModelList(
			AccountRepository.EditSpendViewModelDb editSpendViewModelDb)
		{
			return editSpendViewModelDb?.AccountDataList == null || !editSpendViewModelDb.AccountDataList.Any()
				? new List<EditSpendViewModel>()
				: editSpendViewModelDb.AccountDataList.Select(
					item => CreateEditSpendViewModel(item, editSpendViewModelDb));
		}

		private static EditSpendViewModel CreateEditSpendViewModel(AccountDataResultSet accountDataResultSet,
			AccountRepository.EditSpendViewModelDb editSpendViewModelDb)
		{
			var spendInfo = CreateSpendViewModel(editSpendViewModelDb.SpendViewModelList, accountDataResultSet.AccountId);
			var currencies = CreateCurrencyViewModel(accountDataResultSet.AccountId,
				spendInfo.AmountCurrencyId,
				editSpendViewModelDb.AccountCurrencyList, editSpendViewModelDb.CurrencyDataList,
				editSpendViewModelDb.CurrencyConverterMethodDataList, editSpendViewModelDb.SpendViewModelList);
			var accountIncludeViewModels = CreatAccountIncludeViewModel(accountDataResultSet.AccountId,
				editSpendViewModelDb.SupportedAccountData,
				editSpendViewModelDb
					.CurrencyConverterMethodDataList,
				editSpendViewModelDb.AccountIncludeMethodList);
			var spendTypeViewModels = CreateSpendTypeViewModel(accountDataResultSet.AccountId,
				editSpendViewModelDb.SpendTypeDataList, editSpendViewModelDb.SpendTypeDefaultList);
			accountIncludeViewModels = SetAmountInfoAccountInclude(accountDataResultSet.AccountId,
				accountIncludeViewModels,
				editSpendViewModelDb.SpendIncludeModelList);
			return new EditSpendViewModel
			{
				AccountId = accountDataResultSet.AccountId,
				AccountPeriodId = accountDataResultSet.AccountPeriodId,
				CurrencyId = accountDataResultSet.AccountCurrencyId,
				EndDate = accountDataResultSet.EndDate,
				InitialDate = accountDataResultSet.InitialDate,
				SupportedCurrencies = currencies,
				SupportedAccountInclude = accountIncludeViewModels,
				SpendTypeViewModels = spendTypeViewModels,
				AccountName = accountDataResultSet.AccountName,
				PossibleDateRange = CreateDateRange(editSpendViewModelDb.DateRangeList, accountDataResultSet.AccountId),
				SpendInfo = spendInfo
			};
		}

		private static IEnumerable<AccountIncludeViewModel> SetAmountInfoAccountInclude(int accountId, IEnumerable<AccountIncludeViewModel> supportedAccountInclude,
			IEnumerable<SpendIncludeModelResultSet> spendIncludeModelList)
		{
			if (supportedAccountInclude == null)
			{
				throw new ArgumentNullException(nameof(supportedAccountInclude));
			}

			supportedAccountInclude = supportedAccountInclude.ToList();
			for (var i = 0; i < supportedAccountInclude.Count(); i++)
			{
				var accountIncludeViewModel = supportedAccountInclude.ElementAt(i);
				var spendInfo =
					spendIncludeModelList.FirstOrDefault(
						j => j.AccountId == accountId && j.AccountIncludeId == accountIncludeViewModel.AccountId);
				if (spendInfo != null)
				{
					var spendAmount = new SpendAmount
					{
						CurrencyId = spendInfo.CurrencyId,
						CurrencyName = spendInfo.CurrencyName,
						CurrencySymbol = spendInfo.CurrencySymbol,
						Value = spendInfo.ConvertedAmount
					};
					accountIncludeViewModel.Amount = spendAmount;
				}
			}

			return supportedAccountInclude;
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
			AccountRepository.SupportedAccountIncludeViewModelDb supportedAccountIncludeViewModelDb)
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
					CreatAccountIncludeViewModel(accountId, supportedAccountDataResultSets,
						currencyConverterMethodDataResultSets, accountIncludeMethodResultSets)
			};
		}

		private static IEnumerable<AddSpendViewModel> CreateAddSpendViewModelList(
			AccountRepository.AddSpendViewModelDb addSpendViewModelDb)
		{
			return addSpendViewModelDb?.AccountDataList == null || !addSpendViewModelDb.AccountDataList.Any()
				? new List<AddSpendViewModel>()
				: addSpendViewModelDb.AccountDataList.Select(item => CreateAddSpendViewModel(item, addSpendViewModelDb));
		}

		private static AddSpendViewModel CreateAddSpendViewModel(AccountDataResultSet accountDataResultSet,
			AccountRepository.AddSpendViewModelDb addSpendViewModelDb)
		{
			var currencies = CreateCurrencyViewModel(accountDataResultSet.AccountId, accountDataResultSet.AccountCurrencyId,
				addSpendViewModelDb.AccountCurrencyList, addSpendViewModelDb.CurrencyDataList,
				addSpendViewModelDb.CurrencyConverterMethodDataList);
			var spendTypeViewModels = CreateSpendTypeViewModel(accountDataResultSet.AccountId,
				addSpendViewModelDb.SpendTypeDataList, addSpendViewModelDb.SpendTypeDefaultList);
			var accountIncludeViewModels = CreatAccountIncludeViewModel(accountDataResultSet.AccountId,
																		addSpendViewModelDb.SupportedAccountData,
																		addSpendViewModelDb
																			.CurrencyConverterMethodDataList,
																		addSpendViewModelDb.AccountIncludeMethodList);
			var result = new AddSpendViewModel
			{
				AccountId = accountDataResultSet.AccountId,
				AccountPeriodId = accountDataResultSet.AccountPeriodId,
				CurrencyId = accountDataResultSet.AccountCurrencyId,
				EndDate = accountDataResultSet.EndDate,
				InitialDate = accountDataResultSet.InitialDate,
				SupportedCurrencies = currencies,
				SupportedAccountInclude = accountIncludeViewModels,
				SpendTypeViewModels = spendTypeViewModels,
				AccountName = accountDataResultSet.AccountName
			};

			if (!(result.SuggestedDate >= result.InitialDate && result.SuggestedDate < result.EndDate))
			{
				result.SuggestedDate = result.GetUserEndDate();
			}

			return result;
		}

		private static SpendViewModel CreateSpendViewModel(IEnumerable<SpendModelResultSet> spendModelResultSets,
			int accountId)
		{
			var spendModelResultSet = spendModelResultSets.FirstOrDefault(item => item.AccountId == accountId);
			return CreateSpendViewModel(spendModelResultSet);
		}

		private static DateRange CreateDateRange(IEnumerable<DateRangeResultSet> dateRangeResultSets, int accountId)
		{
			var dateRangeResultSet = dateRangeResultSets.FirstOrDefault(item => item.AccountId == accountId);
			return CreateDateRange(dateRangeResultSet);
		}

		private static DateRange CreateDateRange(DateRangeResultSet dateRangeResultSet)
		{
			if (dateRangeResultSet == null)
				throw new ArgumentNullException(nameof(dateRangeResultSet));
			return new DateRange
			{
				ActualDate = dateRangeResultSet.ActualDate,
				EndDate = dateRangeResultSet.EndDate,
				IsDateValid = dateRangeResultSet.IsDateValid,
				IsValid = dateRangeResultSet.IsValid,
				StartDate = dateRangeResultSet.StartDate
			};
		}

		private static SpendViewModel CreateSpendViewModel(SpendModelResultSet spendModelResultSet)
		{
			if (spendModelResultSet == null)
				throw new ArgumentNullException(nameof(spendModelResultSet));
			return new SpendViewModel
			{
				AccountId = spendModelResultSet.AccountId,
				SpendId = spendModelResultSet.SpendId,
				AmountCurrencyId = spendModelResultSet.AmountCurrencyId,
				SpendDate = spendModelResultSet.SpendDate,
				SpendTypeId = spendModelResultSet.SpendTypeId,
				OriginalAmount = spendModelResultSet.OriginalAmount,
				Numerator = spendModelResultSet.Numerator,
				Denominator = spendModelResultSet.Denominator,
				Description = spendModelResultSet.SpendDescription,
				SetPaymentDate = spendModelResultSet.SetPaymentDate,
				IsPending = spendModelResultSet.IsPending,
				AmountTypeId = spendModelResultSet.AmountTypeId
			};
		}

		private static IEnumerable<SpendTypeViewModel> CreateSpendTypeViewModel(int accountId,
			IEnumerable<SpendTypeInfoResultSet> spendTypeDataResultSets,
			IEnumerable<SpendTypeDefaultResultSet> spendTypeDefaultResultSets)
		{
			if (spendTypeDataResultSets == null || !spendTypeDataResultSets.Any())
				return new List<SpendTypeViewModel>();
			var spendTypeViewModelList = new List<SpendTypeViewModel>();
			foreach (var spendTypeViewModel in spendTypeDataResultSets.Select(CreateSpendTypeViewModel))
			{
				spendTypeViewModel.IsDefault =
					spendTypeDefaultResultSets.Any(
						item => item.AccountId == accountId && item.SpendTypeIdDefault == spendTypeViewModel.SpendTypeId);
				spendTypeViewModelList.Add(spendTypeViewModel);
			}
			return spendTypeViewModelList;
		}

		private static SpendTypeViewModel CreateSpendTypeViewModel(SpendTypeInfoResultSet spendTypeDataResultSet)
		{
			return new SpendTypeViewModel
			{
				Description = spendTypeDataResultSet.SpendTypeDescription,
				SpendTypeId = spendTypeDataResultSet.SpendTypeId,
				SpendTypeName = spendTypeDataResultSet.SpendTypeName
			};
		}

		private static IEnumerable<AccountIncludeViewModel> CreatAccountIncludeViewModel(int accountId,
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
					CreatAccountIncludeViewModel(accountId, item, currencyConverterMethodDataResultSets,
												 accountIncludeMethodResultSets));
			return accountIncludeViewModels;
		}

		private static AccountIncludeViewModel CreatAccountIncludeViewModel(int accountId,
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

		private static IEnumerable<CurrencyViewModel> CreateCurrencyViewModel(int accoutId, int accountCurrencyId,
			IEnumerable<AccountCurrencyResultSet>
				accountCurrencyResultSets,
			IEnumerable<CurrencyDataResultSet>
				currencyDataResultSets,
			IEnumerable<CurrencyConverterMethodDataResultSet>
				currencyConverterMethodDataResultSets,
			IEnumerable<SpendModelResultSet> spendModelResultSets = null)
		{
			if (accountCurrencyResultSets == null || currencyDataResultSets == null ||
				currencyConverterMethodDataResultSets == null)
				throw new ArgumentException("Some or all of the IEnumerables are null");
			var currencyIds =
				accountCurrencyResultSets.Where(item => item.AccountId == accoutId).Select(item => item.CurrencyId);
			var currencyViewModels = CreateCurrencyViewModel(currencyIds, accoutId, accountCurrencyId,
				currencyDataResultSets,
				accountCurrencyResultSets,
				currencyConverterMethodDataResultSets,
				spendModelResultSets?.FirstOrDefault(sp => sp.AccountId == accoutId));
			return currencyViewModels;
		}

		private static IEnumerable<CurrencyViewModel> CreateCurrencyViewModel(IEnumerable<int> currencyIds, int accountId, int accountCurrencyId,
			IEnumerable<CurrencyDataResultSet> currencyDataResultSets,
			IEnumerable<AccountCurrencyResultSet> accountCurrencyResultSets,
			IEnumerable<CurrencyConverterMethodDataResultSet> currencyConverterMethodDataResultSets,
			SpendModelResultSet spendModelResultSet = null)
		{
			var list = currencyDataResultSets.Where(dataResultSet => currencyIds.Any(i => i == dataResultSet.CurrencyId));
			return
				list.Select(
					item =>
						CreateCurrencyViewModel(accountId, accountCurrencyId, item, accountCurrencyResultSets,
							currencyConverterMethodDataResultSets, spendModelResultSet));
		}

		private static CurrencyViewModel CreateCurrencyViewModel(int accountId, int accountCurrencyId, CurrencyDataResultSet currencyDataResultSet,
			IEnumerable<AccountCurrencyResultSet> accountCurrencyResultSets,
			IEnumerable<CurrencyConverterMethodDataResultSet> currencyConverterMethodDataResultSets,
			SpendModelResultSet spendModelResultSet = null)
		{
			if (currencyDataResultSet == null)
				throw new ArgumentNullException(nameof(currencyDataResultSet));
			return new CurrencyViewModel
			{
				CurrencyId = currencyDataResultSet.CurrencyId,
				CurrencyName = currencyDataResultSet.Name,
				Isdefault = currencyDataResultSet.CurrencyId == accountCurrencyId,
				Symbol = currencyDataResultSet.Symbol,
				MethodIds =
						CreateMethodId(accountId, currencyDataResultSet.CurrencyId, accountCurrencyResultSets,
									   currencyConverterMethodDataResultSets, spendModelResultSet)
			};
		}

		private static IEnumerable<MethodId> CreateMethodId(int accountId, int currencyId,
			IEnumerable<AccountCurrencyResultSet> accountCurrencyResultSets,
			IEnumerable<CurrencyConverterMethodDataResultSet> currencyConverterMethodDataResultSets,
			SpendModelResultSet spendModelResultSet = null)
		{
			var accMethods =
				accountCurrencyResultSets.Where(item => item.AccountId == accountId && item.CurrencyId == currencyId);
			return
				currencyConverterMethodDataResultSets.Where(
						item => accMethods.Any(item2 => item2.CurrencyConverterMethodId == item.CurrencyConverterMethodId))
					.Select(ccm => CreateMethodId(ccm, spendModelResultSet,
						accMethods.FirstOrDefault(accm => accm.CurrencyConverterMethodId == ccm.CurrencyConverterMethodId)));
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
		
		private IEnumerable<SavedSpend> CreateSavedSpendList(IEnumerable<SavedSpendResultSet> resultSets)
		{
			if (resultSets == null || !resultSets.Any())
			{
				return new SavedSpend[0];
			}

			var list = new List<SavedSpend>();
			foreach (var resultSet in resultSets)
			{
				var saveSpend = list.FirstOrDefault(sp => sp.SpendId == resultSet.SpendId);
				if (saveSpend == null)
				{
					saveSpend = CreateSavedSpend(resultSet);
					list.Add(saveSpend);
				}

				var addSpendAccount = new ClientAddSpendAccount
				{
					AccountId = resultSet.AccountId,
					ConvertionMethodId = resultSet.CurrencyConvertionMethodId
				};

				if (resultSet.IsOriginal)
				{
					saveSpend.OriginalAccountData = addSpendAccount;
				}
				else
				{
					((IList<ClientAddSpendAccount>)saveSpend.IncludedAccounts).Add(addSpendAccount);
				}
			}

			return list;
		}

		private SavedSpend CreateSavedSpend(SavedSpendResultSet savedSpendResultSet)
		{
			if (savedSpendResultSet == null)
			{
				throw new ArgumentNullException(nameof(savedSpendResultSet));
			}

			var result = new SavedSpend
			{
				SpendId = savedSpendResultSet.SpendId,
				Amount = savedSpendResultSet.Amount,
				AmountDenominator = savedSpendResultSet.AmountDenominator,
				AmountNumerator = savedSpendResultSet.AmountNumerator,
				AmountTypeId = savedSpendResultSet.AmountTypeId,
				CurrencyId = savedSpendResultSet.CurrencyId,
				IncludedAccounts = new List<ClientAddSpendAccount>(),
				SpendDate = savedSpendResultSet.SpendDate,
				UserId = savedSpendResultSet.UserId,
				IsPending = savedSpendResultSet.IsPending
			};

			return result;
		}

		private IEnumerable<ClientAddSpendValidationResultSet> GetClientAddSpendValidationResultSet(DataTable dataTable)
		{
			var currencyDataTableParam = new SqlParameter(DatabaseConstants.PAR_CURRENCY_DATATABLE, dataTable);
			var dataSet = ExecuteStoredProcedure(DatabaseConstants.SP_ADD_SPEND_CURRENCY_VALIDATE,
												 currencyDataTableParam);
			return dataSet == null || dataSet.Tables.Count < 1
					   ? new List<ClientAddSpendValidationResultSet>()
					   : ServicesUtils.CreateGenericList(dataSet.Tables[0],
														 ServicesUtils.CreateClientAddSpendValidationResultSet);
		}

		private void ValidateSpendCurrencyConvertibleValues(ISpendCurrencyConvertible spendCurrencyConvertible)
		{
			if (spendCurrencyConvertible == null)
				throw new ArgumentNullException(nameof(spendCurrencyConvertible));
			var accountData =
				spendCurrencyConvertible.IncludedAccounts.Select(
					item => SpendsDataHelper.CreateClientAddSpendCurrencyData(item, spendCurrencyConvertible.CurrencyId));
			var dataTable = CreateClientAddSpendCurrencyDataDataTable(accountData);
			var clientAddSpendValidationResultSet = GetClientAddSpendValidationResultSet(dataTable);
			var invalids = clientAddSpendValidationResultSet.Where(item => !item.IsSuccess);
			if (!invalids.Any())
				return;
			var invalidAccounts = invalids.Select(SpendsDataHelper.CreateAccountCurrencyConverterData);
			throw new InvalidAddSpendCurrencyException(invalidAccounts);
		}

		private static DataTable CreateClientAddSpendCurrencyDataDataTable(
			IEnumerable<ClientAddSpendCurrencyData> clientAddSpendCurrencyDataList)
		{
			var dataTable = new DataTable();
			dataTable.Columns.Add("AmountCurrencyId");
			dataTable.Columns.Add("AccountId");
			dataTable.Columns.Add("CurrencyConverterMethodId");
			foreach (var data in clientAddSpendCurrencyDataList)
			{
				dataTable.Rows.Add(data.AmountCurrencyId, data.AccountId, data.CurrencyConverterMethodId);
			}
			return dataTable;
		}

		private async Task<AddSpendDbValues> CreateAddSpendDbValuesAsync(ClientAddSpendModel clientAddSpendModel)
		{
			if (clientAddSpendModel == null)
				throw new ArgumentNullException(nameof(clientAddSpendModel));
			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(clientAddSpendModel);
			var accountCurrencyPairList = await GetAccountsCurrencyAsync(accountIds);
			var addSpendDbValues = new AddSpendDbValues
			{
				Amount = clientAddSpendModel.Amount,
				CurrencyId = clientAddSpendModel.CurrencyId,
				SpendDate = clientAddSpendModel.SpendDate,
				SpendTypeId = clientAddSpendModel.SpendTypeId,
				UserId = clientAddSpendModel.UserId,
				IncludedAccounts = await _trxExchangeService.ConvertTrxCurrencyAsync(clientAddSpendModel, accountCurrencyPairList.ToList()),
				AmountDenominator = clientAddSpendModel.AmountDenominator,
				AmountNumerator = clientAddSpendModel.AmountNumerator
			};
			return addSpendDbValues;
		}

		private static AccountFinanceViewModel CreateAccountFinanceViewModel(DataTable dataTable)
		{
			var accountResultSets = ServicesUtils.CreateGenericList(dataTable,
				ServicesUtils.CreateAccountFinanceResultSet);
			var accountModels = CreateAccountFinanceViewModel(accountResultSets);
			return accountModels.Any() ? accountModels.First() : null;
		}

		private static IEnumerable<AccountFinanceViewModel> CreateAccountFinanceViewModel(
			IEnumerable<AccountFinanceResultSet> accountFinanceResultSets)
		{
			if (accountFinanceResultSets == null || !accountFinanceResultSets.Any())
				return new List<AccountFinanceViewModel>();
			var accountFinanceViewModelList = new List<AccountFinanceViewModel>();
			foreach (var resultSet in accountFinanceResultSets)
			{
				var accountFinanceViewModel =
					accountFinanceViewModelList.FirstOrDefault(item => item.AccountPeriodId == resultSet.AccountPeriodId);
				if (accountFinanceViewModel == null)
				{
					accountFinanceViewModel = CreateAccountFinanceViewModel(resultSet);
					accountFinanceViewModelList.Add(accountFinanceViewModel);
				}
				var spendViewModel = CreateSpendViewModel(resultSet);
				if (spendViewModel != null)
					accountFinanceViewModel.SpendViewModels.Add(CreateSpendViewModel(resultSet));
			}
			return accountFinanceViewModelList;
		}

		private static SpendViewModel CreateSpendViewModel(AccountFinanceResultSet resultSet)
		{
			if (resultSet == null)
				throw new ArgumentNullException(nameof(resultSet));
			if (resultSet.SpendId <= 0)
			{
				return null;
			}
			return new SpendViewModel
			{
				AccountId = resultSet.AccountId,
				CurrencyName = resultSet.SpendCurrencyName,
				CurrencySymbol = resultSet.SpendCurrencySymbol,
				Denominator = resultSet.Denominator,
				Numerator = resultSet.Numerator,
				OriginalAmount = resultSet.SpendAmount,
				SpendDate = resultSet.SpendDate,
				SpendId = resultSet.SpendId,
				SpendTypeName = resultSet.SpendTypeName,
				AmountTypeId = resultSet.AmountType,
				SetPaymentDate = resultSet.SetPaymentDate,
				IsPending = resultSet.IsPending,
				Description = resultSet.SpendDescription
			};
		}

		private static AccountFinanceViewModel CreateAccountFinanceViewModel(AccountFinanceResultSet resultSet)
		{
			if (resultSet == null)
				throw new ArgumentNullException(nameof(resultSet));
			return new AccountFinanceViewModel
			{
				AccountId = resultSet.AccountId,
				AccountName = resultSet.AccountName,
				AccountPeriodId = resultSet.AccountPeriodId,
				Budget = resultSet.Budget,
				CurrencyId = resultSet.AccountCurrencyId,
				CurrencySymbol = resultSet.AccountCurrencySymbol,
				InitialDate = resultSet.InitialDate,
				EndDate = resultSet.EndDate,
				GeneralBalance = resultSet.GeneralBalance,
				SpendViewModels = new List<SpendViewModel>(),
				PeriodBalance = resultSet.PeriodBalance,
				Spent = resultSet.AccountPeriodSpent,
				GeneralBalanceToday = resultSet.GeneralBalanceToday
			};

		}

		#endregion
	}
}