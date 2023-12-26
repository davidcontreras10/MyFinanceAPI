using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using DContre.MyFinance.StUtilities;
using MyFinanceBackend.Constants;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ut = DContre.MyFinance.StUtilities.SystemDataUtilities;
using MyFinanceBackend.Models;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceBackend.Services
{
	internal static class ServicesUtils
	{
		public static IEnumerable<T> CreateGenericList<T>(DataTable dataTable, Func<DataRow, T> createMethod)
		{
			return dataTable?.Rows.Cast<DataRow>().Select(createMethod) ?? Array.Empty<T>();
		}

		public static IEnumerable<T> SmartConcat<T>(params IEnumerable<T>[] arrayList) where T : ItemModified, new()
		{
			var concatedlist = new List<T>();
			foreach(var list in arrayList)
			{
				foreach(var item in list)
				{
					if (concatedlist.All(item2 => !item.Equals(item2)))
					{
						concatedlist.Add(item);
					}
				}
			}

			return concatedlist;
		}

		public static DataTable CreateIntDataTable(IEnumerable<int> values)
		{
			var dataTable = new DataTable();
			dataTable.Columns.Add("Value");
			if (values == null)
			{
				return dataTable;
			}

			foreach (var value in values)
			{
				dataTable.Rows.Add(value);
			}

			return dataTable;
		}

		public static BasicScheduledTaskVm CreateBasicScheduledTaskVm(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var value = CreateScheduledTaskVm<BasicScheduledTaskVm>(dataRow);
			value.IsSpend = dataRow.ToBool(DatabaseConstants.COL_IS_SPEND_TRX);
			return value;
		}

		public static TransferScheduledTaskVm CreateTransferScheduledTaskVm(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var value = CreateScheduledTaskVm<TransferScheduledTaskVm>(dataRow);
			value.ToAccountId = dataRow.ToInt(DatabaseConstants.COL_TO_ACCOUNT_ID);
			value.ToAccountName = dataRow.ToString(DatabaseConstants.COL_TO_ACCOUNT_NAME);
			return value;
		}

		private static T CreateScheduledTaskVm<T>(DataRow dataRow) where T : BaseScheduledTaskVm, new()
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new T
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID),
				Description = dataRow.ToString(DatabaseConstants.COL_TASK_DESCRIPTION),
				SpendTypeId = dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID),
				CurrencyId = dataRow.ToInt(DatabaseConstants.COL_CURRENCY_ID),
				Amount = dataRow.ToFloat(DatabaseConstants.COL_AMOUNT),
				Days = CreateArrayFromStringArray(dataRow, DatabaseConstants.COL_DAYS),
				FrequencyType = (ScheduledTaskFrequencyType) dataRow.ToInt(DatabaseConstants.COL_FREQ_TYPE),
				Id = dataRow.ToGuid(DatabaseConstants.COL_AUTOMATIC_TASK_ID),
				CurrencySymbol = dataRow.ToString(DatabaseConstants.COL_CURRENCY_SYMBOL),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				LastExecutedMsg = dataRow.ToString(DatabaseConstants.COL_LAST_EXECUTION_MSG),
				LastExecutedStatus = (ExecutedTaskStatus) dataRow.ToInt(DatabaseConstants.COL_LAST_EXECUTION_STATUS)
			};
		}

		public static ExecutedTaskViewModel CreateExecutedTaskViewModel(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new ExecutedTaskViewModel
			{
				ExecutedDate = dataRow.ToDateTime(DatabaseConstants.COL_EXECUTED_DATE),
				Status = (ExecutedTaskStatus) dataRow.ToInt(DatabaseConstants.COL_EXECUTION_STATUS),
				Message = dataRow.ToString(DatabaseConstants.COL_EXECUTION_MSG)
			};
		}

		public static BankAccountPeriodBasicId CreateBankAccountPeriodBasicId(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new BankAccountPeriodBasicId
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountPeriodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_PERIOD_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				FinancialEntityId = GetNullableInt(dataRow, DatabaseConstants.COL_FINANCIAL_ENTITY_ID),
				FinancialEntityName = dataRow.ToString(DatabaseConstants.COL_FINANCIAL_ENTITY_NAME, DataRowConvert.ParseBehaviorOption.DefaultValue)
			};
		}

		public static AccountPeriodBasicId CreateAccountPeriodBasicId(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountPeriodBasicId
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountPeriodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_PERIOD_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
			};
		}

		public static AccountBasicInfo CreateAccountBasicInfo(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new AccountBasicInfo
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME)
			};

			return result;
		}

		public static ResourceAccessReportRow CreateResourceAccessReportRow(DataRow dataRow)
		{
			if(dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new ResourceAccessReportRow
			{
				ApplicationModuleId = dataRow.ToInt(DatabaseConstants.COL_APPLICATION_MODULE_ID),
				ApplicationModuleName = dataRow.ToString(DatabaseConstants.COL_APPLICATION_MODULE_NAME),
				ApplicationResourceId = dataRow.ToInt(DatabaseConstants.COL_APPLICATION_RESOURCE_ID),
				ApplicationResourceName = dataRow.ToString(DatabaseConstants.COL_APPLICATION_RESOURCE_NAME),
				ResourceAccessLevelId = dataRow.ToInt(DatabaseConstants.COL_RESOURCE_ACCESS_LEVEL_ID),
				ResourceAccessLevelName = dataRow.ToString(DatabaseConstants.COL_RESOURCE_ACCESS_LEVEL_NAME),
				ResourceActionId = dataRow.ToInt(DatabaseConstants.COL_RESOURCE_ACTION_ID),
				ResourceActionName = dataRow.ToString(DatabaseConstants.COL_RESOURCE_ACTION_NAME)
			};

			return result;
		}

		public static DetailLoanResultSet CreateDetailLoanResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new DetailLoanResultSet
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				LoanRecordId = dataRow.ToInt(DatabaseConstants.COL_LOAN_RECORD_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				LoanRecordName = dataRow.ToString(DatabaseConstants.COL_LOAN_RECORD_NAME),
				PaymentSummary = dataRow.ToFloat(DatabaseConstants.COL_PAYMENT_SUMMARY),
				LoanSpendRecord = CreateSpendViewModel(dataRow, "Loan")
			};

			var isSpendIdNull = dataRow.IsDbNull(DatabaseConstants.COL_SPEND_ID);
			if (!isSpendIdNull)
			{
				result.Spend = CreateSpendViewModel(dataRow);
			}

			return result;
		}

		public static SpendActionAttributes CreateSpendAttributes(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new SpendActionAttributes
			{
				IsLoan = dataRow.ToBool(DatabaseConstants.COL_IS_LOAN),
				IsTransfer = dataRow.ToBool(DatabaseConstants.COL_IS_TRANSFER),
				SpendId = dataRow.ToInt(DatabaseConstants.COL_SPEND_ID)
			};

			return result;
		}

		public static SpendViewModel CreateSpendViewModel(DataRow dataRow, string columnPrefix = "")
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new SpendViewModel
			{
				SpendId = dataRow.ToInt(columnPrefix + DatabaseConstants.COL_SPEND_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountId = dataRow.ToInt(columnPrefix + DatabaseConstants.COL_ACCOUNT_ID),
				AmountCurrencyId = dataRow.ToInt(columnPrefix + DatabaseConstants.COL_CURRENCY_ID),
				CurrencyName = dataRow.ToString(columnPrefix + DatabaseConstants.COL_CURRENCY_NAME),
				CurrencySymbol = dataRow.ToString(columnPrefix + DatabaseConstants.COL_CURRENCY_SYMBOL),
				AmountTypeId = dataRow.ToInt(columnPrefix + DatabaseConstants.COL_AMOUNT_TYPE_ID),
				OriginalAmount = dataRow.ToFloat(columnPrefix + DatabaseConstants.COL_ORIGINAL_AMOUNT),
				Description = dataRow.ToString(columnPrefix + DatabaseConstants.COL_DESCRIPTION),
				Denominator = dataRow.ToFloat(columnPrefix + DatabaseConstants.COL_DENOMINATOR),
				Numerator = dataRow.ToFloat(columnPrefix + DatabaseConstants.COL_NUMERATOR),
				IsPending = dataRow.ToBool(columnPrefix + DatabaseConstants.COL_IS_PENDING),
				SetPaymentDate = dataRow.IsDbNull(columnPrefix + DatabaseConstants.COL_SPEND_SET_PAYMENT_DATE) ?
					null :
				   (DateTime?)dataRow.ToDateTime(columnPrefix + DatabaseConstants.COL_SPEND_SET_PAYMENT_DATE),
				SpendDate = dataRow.ToDateTime(columnPrefix + DatabaseConstants.COL_SPEND_DATE),
				SpendTypeId = dataRow.ToInt(columnPrefix + DatabaseConstants.COL_SPEND_TYPE_ID),
				SpendTypeName = dataRow.ToString(columnPrefix + DatabaseConstants.COL_SPEND_TYPE_NAME),
			};

			return result;
		}

		#region Account group

		public static AccountGroupDetailResultSet CreateAccountGroupDetailResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountGroupDetailResultSet
			{
				AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID),
				AccountGroupName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_NAME),
				AccountGroupDisplayValue = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_DISPLAY_VALUE),
				AccountGroupPosition = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_POSITION),
				DisplayDefault = dataRow.ToBool(DatabaseConstants.COL_ACCOUNT_GROUP_DISPLAY_DEFAULT)
			};

		}

		public static AccountGroupResultSet CreateAccountGroupResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountGroupResultSet
			{
				AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID),
				AccountGroupName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_NAME)
			};
		} 

		#endregion

		#region Accounts

		#region Account result set

		public static AccountGroupViewModel CreateAccountGroupViewModel(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountGroupViewModel
			{
				AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID),
				AccountGroupName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_NAME),
				AccountGroupPosition = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_POSITION),
				AccountGroupDisplayValue = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_DISPLAY_VALUE),
				IsSelected = dataRow.ToBool(DatabaseConstants.COL_IS_SELECTED)
			};
		}

		public static AccountIncludeAccountInfoResultSet CreateAccountIncludeAccountInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountIncludeAccountInfoResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				AccountIncludeId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_INCLUDE_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountIncludeName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_INCLUDE_NAME),
				CurrencyConverterMethodId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				CurrencyConverterMethodName =
					dataRow.ToString(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_NAME),
				FinancialEntityId =
					dataRow.ToInt(DatabaseConstants.COL_FINANCIAL_ENTITY_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				FinancialEntityName = dataRow.ToString(DatabaseConstants.COL_FINANCIAL_ENTITY_NAME),
				IsDefault = dataRow.ToBool(DatabaseConstants.COL_IS_DEFAULT),
				IsSelected = dataRow.ToBool(DatabaseConstants.COL_IS_SELECTED)
			};
		}

		public static AccountIncludeInfoResultSet CreateAccountIncludeInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountIncludeInfoResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				CurrencyConverterMethodId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				CurrencyConverterMethodName =
					dataRow.ToString(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_NAME),
				FinancialEntityId =
					dataRow.ToInt(DatabaseConstants.COL_FINANCIAL_ENTITY_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				FinancialEntityName = dataRow.ToString(DatabaseConstants.COL_FINANCIAL_ENTITY_NAME),
				IsDefault = dataRow.ToBool(DatabaseConstants.COL_IS_DEFAULT)
			};
		}

		public static AccountAddViewModelResultSet CreateAccountAddViewModelResultSet(DataSet dataSet)
		{
			if (dataSet == null)
			{
				throw new ArgumentNullException(nameof(dataSet));
			}

			if (dataSet.Tables.Count < AccountAddViewModelResultSet.TABLE_COUNT)
			{
				throw new ArgumentException("Less tables than expected.");
			}

			return new AccountAddViewModelResultSet
			{
				SpendTypeAccountDataResultSetList =
					dataSet.Tables[0].Rows.Cast<DataRow>().Select(row => row.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID)),
				AccountInfoResultSetList = CreateGenericList(dataSet.Tables[1], CreateAccountInfoResultSet),
				SpendTypeInfoResultSetList = CreateGenericList(dataSet.Tables[2], CreateSpendTypeDataResultSet),
				AccountTypeInfoResultSetList = CreateGenericList(dataSet.Tables[3], CreateAccountTypeInfoResultSet),
				PeriodTypeInfoResultSet = CreateGenericList(dataSet.Tables[4], CreatePeriodTypeInfoResultSet),
				FinancialEntityInfoResultSetList =
					CreateGenericList(dataSet.Tables[5], CreateFinancialEntityInfoResultSet),
				CurrencyDataResultSetList = CreateGenericList(dataSet.Tables[6], CreateCurrencyDataResultSet),
				AccountIncludeAccountInfoResultSetList =
					CreateGenericList(dataSet.Tables[7], CreateAccountIncludeAccountInfoResultSet),
				AccountGroupResultSetList = CreateGenericList(dataSet.Tables[8], CreateAccountGroupResultSet)
			};
		}

		public static AccountEditViewModelResultSet CreateAccountEditViewModelResultSet(DataSet dataSet)
		{
			if (dataSet == null)
			{
				throw new ArgumentNullException(nameof(dataSet));
			}

			if (dataSet.Tables.Count < AccountEditViewModelResultSet.TABLE_COUNT)
			{
				throw new ArgumentException("Less tables than expected.");
			}

			return new AccountEditViewModelResultSet
			{
				AccountDetailResultSetList = CreateGenericList(dataSet.Tables[0], CreateAccountDetailResultSet),
				SpendTypeAccountDataResultSetList =
					CreateGenericList(dataSet.Tables[1], CreateSpendTypeAccountDataResultSet),
				AccountInfoResultSetList = CreateGenericList(dataSet.Tables[2], CreateAccountInfoResultSet),
				SpendTypeInfoResultSetList = CreateGenericList(dataSet.Tables[3], CreateSpendTypeDataResultSet),
				AccountTypeInfoResultSetList = CreateGenericList(dataSet.Tables[4], CreateAccountTypeInfoResultSet),
				PeriodTypeInfoResultSet = CreateGenericList(dataSet.Tables[5], CreatePeriodTypeInfoResultSet),
				FinancialEntityInfoResultSetList =
					CreateGenericList(dataSet.Tables[6], CreateFinancialEntityInfoResultSet),
				CurrencyDataResultSetList = CreateGenericList(dataSet.Tables[7], CreateCurrencyDataResultSet),
				AccountIncludeAccountInfoResultSetList =
					CreateGenericList(dataSet.Tables[8], CreateAccountIncludeAccountInfoResultSet),
				AccountGroupResultSetList = CreateGenericList(dataSet.Tables[9], CreateAccountGroupResultSet)
			};
		}

		public static AccountDetailResultSet CreateAccountDetailResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountDetailResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				AccountPosition = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_POSITION),
				AccountHeaderColor = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_HEADER_COLOR),
				CurrencyId =
					dataRow.ToInt(DatabaseConstants.COL_CURRENCY_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				FinancialEntityId =
					dataRow.ToInt(DatabaseConstants.COL_FINANCIAL_ENTITY_ID),
				PeriodDefinitionId =
					dataRow.ToInt(DatabaseConstants.COL_PERIOD_DEFINITION_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountTypeId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_TYPE_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				SpendTypeId =
					dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID),
				BaseBudget = dataRow.ToFloat(DatabaseConstants.COL_BASE_BUDGET),
				AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID)
			};
		}

		public static AccountDetailsPeriodViewModel CreateAccountDetailsPeriodViewModel(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountDetailsPeriodViewModel
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				AccountPosition = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_POSITION),
				BaseBudget = dataRow.ToFloat(DatabaseConstants.COL_BASE_BUDGET),
				AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID),
				AccountPeriodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_PERIOD_ID),
				GlobalOrder = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_POSITION)
			};
		}

		public static SpendTypeAccountDataResultSet CreateSpendTypeAccountDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new SpendTypeAccountDataResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				SpendTypeId =
					dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID, DataRowConvert.ParseBehaviorOption.ThrowException)
			};
		}

		public static AccountInfoResultSet CreateAccountInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountInfoResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME)
			};
		}

		public static AccountTypeInfoResultSet CreateAccountTypeInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountTypeInfoResultSet
			{
				AccountTypeId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_TYPE_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountTypeName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_TYPE_NAME)
			};
		}

		public static PeriodTypeInfoResultSet CreatePeriodTypeInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var repetition = dataRow.ToInt(DatabaseConstants.COL_REPETITION);
			repetition = repetition != 0 ? repetition : 1;
			return new PeriodTypeInfoResultSet
			{
				PeriodDefinitionId =
					dataRow.ToInt(DatabaseConstants.COL_PERIOD_DEFINITION_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				CuttingDate =
					dataRow.ToString(DatabaseConstants.COL_CUTTING_DATE,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				PeriodTypeId =
					dataRow.ToInt(DatabaseConstants.COL_PERIOD_TYPE_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				PeriodTypeName = dataRow.ToString(DatabaseConstants.COL_PERIOD_TYPE_NAME),
				Repetition = repetition
			};
		}

		public static FinancialEntityInfoResultSet CreateFinancialEntityInfoResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new FinancialEntityInfoResultSet
			{
				FinancialEntityId =
					dataRow.ToInt(DatabaseConstants.COL_FINANCIAL_ENTITY_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				FinancialEntityName = dataRow.ToString(DatabaseConstants.COL_FINANCIAL_ENTITY_NAME)
			};
		}

		#endregion

		#endregion

		public static AccountBasicPeriodInfo CreateAccountBasicPeriodInfo(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new AccountBasicPeriodInfo
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				MinDate = dataRow.ToDateTime(DatabaseConstants.COL_MIN_DATE),
				MaxDate = dataRow.ToDateTime(DatabaseConstants.COL_MAX_DATE)
			};

			return result;
		}

		public static AccountPeriodBasicInfo CreateAccountPeriodBasicInfo(DataRow dataRow)
		{
			var accountPeriod = CreateAccountPeriod(dataRow);
			var accountPeriodBasicInfo = new AccountPeriodBasicInfo
			{
				AccountPeriodId = accountPeriod.AccountPeriodId,
				AccountId = accountPeriod.AccountId,
				Budget = accountPeriod.Budget,
				InitialDate = accountPeriod.InitialDate,
				EndDate = accountPeriod.EndDate,
				AccountName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_NAME),
				UserId = dataRow.ToString(DatabaseConstants.COL_USER_ID)
			};

			return accountPeriodBasicInfo;
		}

		public static AccountPeriod CreateAccountPeriod(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new AccountPeriod
			{
				AccountPeriodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_PERIOD_ID,
					DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID),
				Budget = dataRow.ToFloat(DatabaseConstants.COL_BUDGET),
				EndDate = dataRow.ToDateTime(DatabaseConstants.COL_END_DATE),
				InitialDate = dataRow.ToDateTime(DatabaseConstants.COL_INITIAL_DATE)
			};

			return result;
		}

		public static SavedSpendResultSet CreateSavedSpend(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new SavedSpendResultSet
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID),
				SpendId = dataRow.ToInt(DatabaseConstants.COL_SPEND_ID),
				Amount = dataRow.ToFloat(DatabaseConstants.COL_AMOUNT),
				AmountDenominator = dataRow.ToFloat(DatabaseConstants.COL_AMOUNT_DENOMINATOR),
				AmountNumerator = dataRow.ToFloat(DatabaseConstants.COL_AMOUNT_NUMERATOR),
				AmountTypeId = (TransactionTypeIds)dataRow.ToInt(DatabaseConstants.COL_AMOUNT_TYPE_ID),
				CurrencyConvertionMethodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID),
				CurrencyId = dataRow.ToInt(DatabaseConstants.COL_CURRENCY_ID),
				IsOriginal = dataRow.ToBool(DatabaseConstants.COL_IS_ORIGINAL),
				SpendDate = dataRow.ToDateTime(DatabaseConstants.COL_SPEND_DATE),
				UserId = dataRow.ToString(DatabaseConstants.COL_USER_ID),
				IsPending = dataRow.ToBool(DatabaseConstants.COL_IS_PENDING)
			};

			return result;
		}

		public static UserAssignedAccess CreateUserAssignedAccess(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var result = new UserAssignedAccess
			{
				UserId = dataRow.ToString(DatabaseConstants.COL_USER_ID),
				ApplicationResource =
					GetEnum<ApplicationResources>(dataRow, DatabaseConstants.COL_APPLICATION_RESOURCE_ID),
				ResourceAccesLevel =
					GetEnum<ResourceAccesLevels>(dataRow, DatabaseConstants.COL_RESOURCE_ACCESS_LEVEL_ID),
				ResourceActionName = GetEnum<ResourceActionNames>(dataRow, DatabaseConstants.COL_RESOURCE_ACTION_ID)
			};

			return result;
		}

		public static string CreateFrontStyleDataJson(FrontStyleData frontStyleData)
		{
			if (frontStyleData == null)
				return "";

			var result = JsonConvert.SerializeObject(frontStyleData);
			return result;
		}

		public static string CreateClientAccountIncludeJson(IEnumerable<ClientAccountInclude> clientAccountIncludes)
		{
			if (clientAccountIncludes == null || !clientAccountIncludes.Any())
			{
				return "";
			}

			var result = JsonConvert.SerializeObject(clientAccountIncludes);
			return result;
		}

		public static string CreateClientAccountPositionToJson(IEnumerable<ClientAccountPosition> accountPositions)
		{
			if (accountPositions == null || !accountPositions.Any())
			{
				throw new ArgumentNullException(nameof(accountPositions));
			}

			return JsonConvert.SerializeObject(accountPositions);
		}

		public static SpendTypeViewModel CreateSpendTypeViewModel(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new SpendTypeViewModel
			{
				SpendTypeId = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_TYPE_ID),
				Description = Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_DESCRIPTION),
				IsDefault = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_DEFAULT),
				SpendTypeName = Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_NAME)
			};
		}

		public static AccountMethodConvertionInfo CreateAccountMethodConvertionInfo(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new AccountMethodConvertionInfo
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				CurrencyConverterMethodId =
					Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID)
			};
		}

		//public static AccountRepository.SupportedAccountIncludeViewModelDb CreateSupportedAccountIncludeViewModelDb(DataSet dataSet)
		//{
		//	if (dataSet == null || dataSet.Tables.Count < 4)
		//		throw new Exception("Invalid dataSet for SupportedAccountIncludeViewModelDb");
		//	return new AccountRepository.SupportedAccountIncludeViewModelDb
		//	{
		//		SupportedAccountData = CreateGenericList(dataSet.Tables[0], CreateSupportedAccountDataResultSet),
		//		CurrencyConverterMethodDataList =
		//			CreateGenericList(dataSet.Tables[1], CreateCurrencyConverterMethodDataResultSet),
		//		AccountIncludeMethodList = CreateGenericList(dataSet.Tables[2], CreateAccountIncludeMethodResultSet),
		//		AccountIdList = CreateGenericList(dataSet.Tables[3], CreateAccountId)
		//	};
		//}

		public static FrontStyleData CreateFrontStyleData(string json)
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
					BorderColor = (string) jObject["borderColor".ToUpper()],
					HeaderColor = (string) jObject["headerColor".ToUpper()]
				};
			}
			catch (JsonReaderException)
			{
				return new FrontStyleData();
			}
		}

		private static int CreateAccountId(DataRow dataRow)
		{
			return dataRow == null ? 0 : Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID);
		}

		public static AccountViewModel CreateAccountViewModel(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));

			return new AccountViewModel
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				AccountName = Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_NAME),
				GlobalOrder = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GLOBAL_ORDER)
			};
		} 

		public static ClientAddSpendValidationResultSet CreateClientAddSpendValidationResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new ClientAddSpendValidationResultSet
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					AccountName = Ut.GetString(dataRow,DatabaseConstants.COL_ACCOUNT_NAME),
					AmountCurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_AMOUNT_CURRENCY_ID),
					ConvertCurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CONVERT_CURRENCY_ID),
					CurrencyIdOne = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID_ONE),
					CurrencyIdTwo = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID_TWO),
					IsSuccess = Ut.GetBool(dataRow, DatabaseConstants.IS_SUCCESS)
				};
		}

		#region EditSpend ViewModel

		//public static AccountRepository.EditSpendViewModelDb CreateEditSpendViewModelDb(DataSet dataSet)
		//{
		//	if (dataSet == null || dataSet.Tables.Count < 10)
		//		throw new ArgumentException(@"This dataset can't create EditSpendViewModelDb", nameof(dataSet));
		//	return new AccountRepository.EditSpendViewModelDb
		//	{
		//		AccountCurrencyList = CreateGenericList(dataSet.Tables[0], CreateAccountCurrencyResultSet),
		//		AccountIncludeMethodList = CreateGenericList(dataSet.Tables[1], CreateAccountIncludeMethodResultSet),
		//		AccountIncludeDataList = CreateGenericList(dataSet.Tables[2], CreateAccountIncludeDataResultSet),
		//		SupportedAccountData = CreateGenericList(dataSet.Tables[3], CreateSupportedAccountDataResultSet),
		//		CurrencyConverterMethodDataList =
		//			CreateGenericList(dataSet.Tables[4], CreateCurrencyConverterMethodDataResultSet),
		//		CurrencyDataList = CreateGenericList(dataSet.Tables[5], CreateCurrencyDataResultSet),
		//		AccountDataList = CreateGenericList(dataSet.Tables[6], CreateAccountDataResultset),
		//		SpendTypeDataList = CreateGenericList(dataSet.Tables[7], CreateSpendTypeDataResultSet),
		//		SpendTypeDefaultList = CreateGenericList(dataSet.Tables[8], CreateSpendTypeDefaultResultSet),
		//		SpendViewModelList = CreateGenericList(dataSet.Tables[9], CreateSpendModelResultSet),
		//		DateRangeList = CreateGenericList(dataSet.Tables[10], CreateDateRangeResultSet),
		//		SpendIncludeModelList = CreateGenericList(dataSet.Tables[11], CreateSpendIncludeModelResultSet)
		//	};
		//}

		public static SpendIncludeModelResultSet CreateSpendIncludeModelResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new SpendIncludeModelResultSet
			{
				AccountId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountPeriodId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_PERIOD_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				AccountIncludeId =
					dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_INCLUDE_ID,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				CurrencyId =
					dataRow.ToInt(DatabaseConstants.COL_CURRENCY_ID, DataRowConvert.ParseBehaviorOption.ThrowException),
				CurrencyName =
					dataRow.ToString(DatabaseConstants.COL_CURRENCY_NAME,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				CurrencySymbol =
					dataRow.ToString(DatabaseConstants.COL_CURRENCY_SYMBOL,
						DataRowConvert.ParseBehaviorOption.ThrowException),
				ConvertedAmount =
					dataRow.ToFloat(DatabaseConstants.COL_CONVERTED_AMOUNT, DataRowConvert.ParseBehaviorOption.ThrowException),
				IsSelected = dataRow.ToBool(DatabaseConstants.COL_IS_SELECTED)
			};
		}

		public static DateRangeResultSet CreateDateRangeResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new DateRangeResultSet
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				ActualDate = dataRow.ToDateTime(DatabaseConstants.COL_ACTUAL_DATE),
				StartDate = dataRow.ToDateTime(DatabaseConstants.COL_MIN_DATE),
				EndDate = dataRow.ToDateTime(DatabaseConstants.COL_MAX_DATE),
				IsDateValid = dataRow.ToBool(DatabaseConstants.COL_IS_DATE_VALID),
				IsValid = dataRow.ToBool(DatabaseConstants.COL_IS_VALID)
			};
		}

		public static SpendModelResultSet CreateSpendModelResultSet(DataRow dataRow)
		{
			var model = new SpendModelResultSet
			{
				AccountId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_ID),
				AmountCurrencyId = dataRow.ToInt(DatabaseConstants.COL_AMOUNT_CURRENCY_ID),
				Denominator = dataRow.ToInt(DatabaseConstants.COL_DENOMINATOR),
				Numerator = dataRow.ToInt(DatabaseConstants.COL_NUMERATOR),
				OriginalAmount = dataRow.ToFloat(DatabaseConstants.COL_ORIGINAL_AMOUNT),
				SpendDate = dataRow.ToDateTime(DatabaseConstants.COL_SPEND_DATE),
				SpendDescription = dataRow.ToString(DatabaseConstants.COL_SPEND_DESCRIPTION),
				SpendId = dataRow.ToInt(DatabaseConstants.COL_SPEND_ID),
				SpendTypeId = dataRow.ToInt(DatabaseConstants.COL_SPEND_TYPE_ID),
				CurrencyConverterMethodId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID),
				SetPaymentDate = dataRow.ToDateTime(DatabaseConstants.COL_SPEND_SET_PAYMENT_DATE),
				IsPending = dataRow.ToBool(DatabaseConstants.COL_IS_PENDING),
				AmountTypeId = dataRow.ToInt(DatabaseConstants.COL_AMOUNT_TYPE_ID)
			};

			if (model.SetPaymentDate == default(DateTime))
			{
				model.SetPaymentDate = null;
			}

			return model;
		}

		#endregion

		#region AddSpend ViewModel

		//public static AccountRepository.AddSpendViewModelDb CreateAddSpendViewModelDb(DataSet dataSet)
		//{
		//	if (dataSet == null || dataSet.Tables.Count < 8)
		//		throw new ArgumentException(@"This dataSet can't create AddSpendViewModelDb", nameof(dataSet));
		//	return new AccountRepository.AddSpendViewModelDb
		//	{
		//		AccountCurrencyList = CreateGenericList(dataSet.Tables[0], CreateAccountCurrencyResultSet),
		//		AccountIncludeMethodList = CreateGenericList(dataSet.Tables[1], CreateAccountIncludeMethodResultSet),
		//		AccountIncludeDataList = CreateGenericList(dataSet.Tables[2], CreateAccountIncludeDataResultSet),
		//		SupportedAccountData = CreateGenericList(dataSet.Tables[3], CreateSupportedAccountDataResultSet),
		//		CurrencyConverterMethodDataList =
		//			CreateGenericList(dataSet.Tables[4], CreateCurrencyConverterMethodDataResultSet),
		//		CurrencyDataList = CreateGenericList(dataSet.Tables[5], CreateCurrencyDataResultSet),
		//		AccountDataList = CreateGenericList(dataSet.Tables[6], CreateAccountDataResultset),
		//		SpendTypeDataList = CreateGenericList(dataSet.Tables[7], CreateSpendTypeDataResultSet),
		//		SpendTypeDefaultList = CreateGenericList(dataSet.Tables[8], CreateSpendTypeDefaultResultSet)
		//	};
		//}

		public static AccountDataResultSet CreateAccountDataResultset(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new AccountDataResultSet
				{
					AccountCurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID),
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					AccountPeriodId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_ID),
					InitialDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_INITIAL_DATE),
					EndDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_END_DATE),
					AccountName = Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_NAME)
				};
		}

		public static AccountCurrencyResultSet CreateAccountCurrencyResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new AccountCurrencyResultSet
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					CurrencyConverterMethodId =
						Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID),
					CurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID),
					IsSuggested = dataRow.ToBool(DatabaseConstants.COL_IS_SUGGESTED)
				};
		}

		public static AccountIncludeMethodResultSet CreateAccountIncludeMethodResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new AccountIncludeMethodResultSet
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				AccountIncludeId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_INCLUDE_ID),
				IsDefault = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_DEFAULT),
				CurrencyConverterMethodId =
					Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID),
				IsCurrentSelection = dataRow.ToBool(DatabaseConstants.COL_IS_CURRENT_SELECTION)
			};
		}

		public static CurrencyConverterMethodDataResultSet CreateCurrencyConverterMethodDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new CurrencyConverterMethodDataResultSet
				{
					CurrencyConverterMethodId =
						Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID),
					CurrencyConverterMethodName =
						Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_NAME),
					IsDefault = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_DEFAULT)
				};
		}

		public static SupportedAccountDataResultSet CreateSupportedAccountDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new SupportedAccountDataResultSet
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					Name = Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_NAME)
				};
		}

		public static CurrencyDataResultSet CreateCurrencyDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new CurrencyDataResultSet
				{
					CurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID),
					Name = Ut.GetString(dataRow, DatabaseConstants.COL_CURRENCY_NAME),
					Symbol = Ut.GetString(dataRow, DatabaseConstants.COL_CURRENCY_SYMBOL)
				};
		}

		public static SpendTypeInfoResultSet CreateSpendTypeDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new SpendTypeInfoResultSet
				{
					SpendTypeId =
						Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_TYPE_ID),
					SpendTypeName =
						Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_NAME),
					SpendTypeDescription =
						Ut.GetString(dataRow, DatabaseConstants.COL_DESCRIPTION)
				};
		}

		public static AccountIncludeDataResultSet CreateAccountIncludeDataResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new AccountIncludeDataResultSet
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					AccountIncludeId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_INCLUDE_ID),
					CurrencyConverterMethodId =
						Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID)
				};
		}

		public static SpendTypeDefaultResultSet CreateSpendTypeDefaultResultSet(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));
			return new SpendTypeDefaultResultSet
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				SpendTypeIdDefault = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_TYPE_ID)
			};
		}

		#endregion

		public static CurrencyViewModel CreateCurrencyViewModel(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException(nameof(dataRow));

			return new CurrencyViewModel
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				CurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID),
				CurrencyName = Ut.GetString(dataRow, DatabaseConstants.COL_CURRENCY_NAME),
				Isdefault = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_DEFAULT),
				Symbol = Ut.GetString(dataRow, DatabaseConstants.COL_CURRENCY_SYMBOL),
				MethodIds = new List<MethodId>()
			};
		}

		public static string CreateStringCharSeparated(IEnumerable<int> ids)
		{
			return CreateStringCharSeparated(ids, ',');
		}

		public static string CreateStringCharSeparated(IEnumerable<int> ids, char separator)
		{
			if (ids == null)
				return string.Empty;
			var result = ids.Aggregate(string.Empty, (current, i) => current + ("," + i.ToString(CultureInfo.InvariantCulture)));
			if (!string.IsNullOrEmpty(result) && result[0] == separator)
				result = result.Remove(0, 1);
			return result;
		}

		public static IEnumerable<AccountFinanceResultSet> CreateAccountFinanceResultSet(DataTable dataTable)
		{
			return dataTable == null || dataTable.Rows.Count == 0
					   ? new List<AccountFinanceResultSet>()
					   : dataTable.Rows.Cast<DataRow>()
								  .Select(CreateAccountFinanceResultSet)
								  .Where(item => item != null);
		}

		public static AccountFinanceResultSet CreateAccountFinanceResultSet(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			var model = new AccountFinanceResultSet
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				AccountName = Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_NAME),
				AccountCurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_ID),
				AccountCurrencySymbol =
					Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_SYMBOL),
				AccountPeriodId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_ID),
				Budget = Ut.GetFloat(dataRow, DatabaseConstants.COL_BUDGET),
				Numerator = Ut.GetFloat(dataRow, DatabaseConstants.COL_NUMERATOR),
				Denominator = Ut.GetFloat(dataRow, DatabaseConstants.COL_DENOMINATOR),
				InitialDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_INITIAL_DATE),
				EndDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_END_DATE),
				SpendAmount = Ut.GetFloat(dataRow, DatabaseConstants.COL_SPEND_AMOUNT),
				SpendDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_SPEND_DATE),
				SpendId = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_ID),
				SpendTypeName = Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_NAME),
				SpendCurrencyName = Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_CURRENCY_NAME),
				SpendCurrencySymbol = Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_CURRENCY_SYMBOL),
				GeneralBalance = Ut.GetFloat(dataRow, DatabaseConstants.COL_ACCOUNT_GENERAL_BALANCE),
				GeneralBalanceToday = Ut.GetFloat(dataRow, DatabaseConstants.COL_ACCOUNT_GENERAL_BALANCE_TODAY),
				PeriodBalance = Ut.GetFloat(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_BALANCE),
				AccountPeriodSpent = Ut.GetFloat(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_SPENT),
				AmountType = Ut.GetInt(dataRow, DatabaseConstants.COL_AMOUNT_TYPE),
				SetPaymentDate = dataRow.ToDateTime(DatabaseConstants.COL_SPEND_SET_PAYMENT_DATE),
				IsPending = dataRow.ToBool(DatabaseConstants.COL_IS_PENDING),
				SpendDescription = dataRow.ToString(DatabaseConstants.COL_SPEND_DESCRIPTION)
			};

			if (model.SetPaymentDate == default(DateTime))
			{
				model.SetPaymentDate = null;
			}

			return model;
		}

		public static ItemModified CreateAccountAffected(DataRow dataRow)
		{
			return dataRow == null
					   ? null
					   : new ItemModified
						   {
							   AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
							   IsModified = Ut.GetBool(dataRow, DatabaseConstants.COL_AFFECTED)
						   };
		}

		public static SpendItemModified CreateSpendAccountAffected(DataRow dataRow)
		{
			return dataRow == null
				? null
				: new SpendItemModified
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					IsModified = Ut.GetBool(dataRow, DatabaseConstants.COL_AFFECTED),
					SpendId = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_ID)
				};
		}

		public static IEnumerable<ItemModified> CreateAccountAffected(DataSet dataSet)
		{
			return dataSet == null || dataSet.Tables.Count == 0
					   ? new List<ItemModified>()
					   : dataSet.Tables[0].Rows.Cast<DataRow>()
										  .Select(CreateAccountAffected)
										  .Where(item => item != null);
		}

		public static IEnumerable<SpendItemModified> CreateSpendAccountAffected(DataSet dataSet)
		{
			return dataSet == null || dataSet.Tables.Count == 0
					   ? new List<SpendItemModified>()
					   : dataSet.Tables[0].Rows.Cast<DataRow>()
										  .Select(CreateSpendAccountAffected)
										  .Where(item => item != null);
		}

		public static ClientAddSpendAccount CreateClientAddSpendAccount(DataRow dataRow)
		{
			if (dataRow == null)
			{
				throw new ArgumentNullException(nameof(dataRow));
			}

			return new ClientAddSpendAccount
			{
				AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
				ConvertionMethodId =
					Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID)
			};
		}

		public static DataTable ClientAddSpendAccountDataTable(
			IEnumerable<AddSpendAccountDbValues> addSpendAccountDbValues, bool allowNull = false)
		{
			var dataTable = new DataTable();
			dataTable.Columns.Add("AccountId");
			dataTable.Columns.Add("Numerator");
			dataTable.Columns.Add("Denominator");
			dataTable.Columns.Add("PendingUpdate");
			dataTable.Columns.Add("CurrencyConverterMethodId");
			dataTable.Columns.Add("IsOriginal");
			if (addSpendAccountDbValues == null || !addSpendAccountDbValues.Any())
			{
				if (!allowNull)
					throw new ArgumentNullException(nameof(addSpendAccountDbValues));
				return dataTable;
			}

			foreach (var dbValue in addSpendAccountDbValues)
			{
				dataTable.Rows.Add(dbValue.AccountId, dbValue.Numerator, dbValue.Denominator, dbValue.PendingUpdate,
					dbValue.CurrencyConverterMethodId, dbValue.IsOriginal);
			}
			return dataTable;
		}

		public static AccountCurrencyPair CreateAccountCurrencyPair(DataRow dataRow)
		{
			if (dataRow == null)
				return null;
			return new AccountCurrencyPair
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					CurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID)
				};
		}

		private static bool EmptyRowValue(DataRow dataRow, string column)
		{
			return string.IsNullOrEmpty(column) || dataRow?[column] == null || dataRow[column].Equals(DBNull.Value);
		}

		public static DateRange CreateDateRange(DataRow dataRow)
		{
			if (dataRow == null)
				return null;
			var dateRange = new DateRange();
			if (!EmptyRowValue(dataRow, DatabaseConstants.COL_MIN_DATE))
			{
				dateRange.StartDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_MIN_DATE);
			}
			if (!EmptyRowValue(dataRow, DatabaseConstants.COL_MAX_DATE))
			{
				dateRange.EndDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_MAX_DATE);
			}
			if (!EmptyRowValue(dataRow, DatabaseConstants.COL_ACTUAL_DATE))
			{
				dateRange.ActualDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_ACTUAL_DATE);
			}
			if (!EmptyRowValue(dataRow, DatabaseConstants.COL_IS_DATE_VALID))
			{
				dateRange.IsDateValid = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_DATE_VALID);
			}
			if (!EmptyRowValue(dataRow, DatabaseConstants.COL_IS_VALID))
			{
				dateRange.IsValid = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_VALID);
			}
			return dateRange;
		}

		public static AddPeriodData CreateAddPeriodData(DataSet dataSet, int accountId)
		{
			AddPeriodData addPeriodData = new AddPeriodData
				{
					AccountId = accountId
				};
			if (dataSet.Tables.Count == 0 || dataSet.Tables[0].Rows.Count == 0)
				return addPeriodData;
			var isValid = Ut.GetBool(dataSet.Tables[0].Rows[0], DatabaseConstants.COL_IS_VALID);
			addPeriodData.IsValid = isValid;
			if (!isValid)
				return addPeriodData;
			addPeriodData.Budget = Ut.GetFloat(dataSet.Tables[0].Rows[0], DatabaseConstants.COL_BUDGET);
			addPeriodData.NextInitialDate = null;
			addPeriodData.HasPeriods = Ut.GetBool(dataSet.Tables[0].Rows[0], DatabaseConstants.COL_HAS_PERIODS);
			if (!EmptyRowValue(dataSet.Tables[0].Rows[0], DatabaseConstants.COL_END_DATE))
			{
				addPeriodData.NextInitialDate = Ut.GetDateTime(dataSet.Tables[0].Rows[0], DatabaseConstants.COL_END_DATE);
			}

			return addPeriodData;
		}

		public static UserAccountsViewModel CreateMainViewModel(DataSet dataSet)
		{
			if (dataSet == null || dataSet.Tables.Count == 0)
				return new UserAccountsViewModel();
			var mainViewRowList = dataSet.Tables[0].Rows.Cast<DataRow>().Select(CreateMainViewRow).Where(i => i != null);
			var mainViewModel = CreateMainViewModel(mainViewRowList);
			return mainViewModel;
		}

		private static UserAccountsViewModel CreateMainViewModel(IEnumerable<MainViewRow> mainViewRows)
		{
			if (mainViewRows == null || !mainViewRows.Any())
				return new UserAccountsViewModel();
			var accountGroupMainViewViewModelList = new List<AccountGroupMainViewViewModel>();
			foreach (var mainViewRow in mainViewRows)
			{
				var accountGroupMainViewViewModel =
					accountGroupMainViewViewModelList.FirstOrDefault(i => i.AccountGroupId == mainViewRow.AccountGroupId);
				if (accountGroupMainViewViewModel == null)
				{
					accountGroupMainViewViewModel = new AccountGroupMainViewViewModel
					{
						AccountGroupId = mainViewRow.AccountGroupId,
						AccountGroupDisplayValue = mainViewRow.AccountGroupDisplayValue,
						AccountGroupName = mainViewRow.AccountGroupName,
						AccountGroupPosition = mainViewRow.AccountGroupPosition,
						IsSelected = mainViewRow.AccountGroupDisplayDefault
					};

					accountGroupMainViewViewModelList.Add(accountGroupMainViewViewModel);
				}

				var mainViewModelAccounts = new List<FullAccountInfoViewModel>();
				foreach (
					var groupMainViewRow in
						mainViewRows.Where(i => i.AccountGroupId == accountGroupMainViewViewModel.AccountGroupId))
				{
					var row = groupMainViewRow;
					var mainViewModelAccount =
						mainViewModelAccounts.FirstOrDefault(item => item.AccountId == row.AccountId);
					if (mainViewModelAccount == null)
					{
						mainViewModelAccount = CreateMainViewModelAccount(groupMainViewRow);
						mainViewModelAccounts.Add(mainViewModelAccount);
					}

					var accountPeriod = CreateAccountPeriod(groupMainViewRow);
					if (accountPeriod != null)
					{
						mainViewModelAccount.AccountPeriods.Add(accountPeriod);
					}
				}

				accountGroupMainViewViewModel.Accounts = mainViewModelAccounts;
			}

			return new UserAccountsViewModel
			{
				AccountGroupMainViewViewModels = accountGroupMainViewViewModelList
			};
		}

		private static FullAccountInfoViewModel CreateMainViewModelAccount(MainViewRow mainViewRow)
		{
			if (mainViewRow == null)
			{
				throw new ArgumentNullException(nameof(mainViewRow));
			}

			return new FullAccountInfoViewModel
			{
				AccountId = mainViewRow.AccountId,
				AccountName = mainViewRow.AccountName,
				CurrencyId = mainViewRow.CurrencyId,
				CurrencyName = mainViewRow.CurrencyName,
				CurrentPeriodId = mainViewRow.CurrentPeriodId,
				AccountPeriods = new List<AccountPeriod>(),
				FrontStyle = CreateFrontStyleData(mainViewRow.HeaderColor),
				Position = mainViewRow.Position,
				Type = mainViewRow.Type
			};
		}

		private static AccountPeriod CreateAccountPeriod(MainViewRow mainViewRow)
		{
			return mainViewRow == null
					   ? null
					   : new AccountPeriod
						   {
							   AccountId = mainViewRow.AccountId,
							   AccountPeriodId = mainViewRow.AccountPeriodId,
							   InitialDate = mainViewRow.InitialDate,
							   EndDate = mainViewRow.EndDate
						   };
		}

		private static MainViewRow CreateMainViewRow(DataRow dataRow)
		{
			return dataRow == null
				? null
				: new MainViewRow
				{
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					AccountName = Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_NAME),
					AccountPeriodId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_ID),
					EndDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_END_DATE),
					InitialDate = Ut.GetDateTime(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_INITIAL_DATE),
					Position = Ut.GetInt(dataRow, DatabaseConstants.COL_POSITION),
					CurrencyId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENCY_ID),
					CurrencyName = Ut.GetString(dataRow, DatabaseConstants.COL_CURRENCY_NAME),
					CurrentPeriodId = Ut.GetInt(dataRow, DatabaseConstants.COL_CURRENT_PERIOD_ID),
					HeaderColor = Ut.GetString(dataRow, DatabaseConstants.COL_HEADER_COLOR),
					Type =
						CreateAccountType(dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_TYPE_ID,
							DataRowConvert.ParseBehaviorOption.ThrowException)),
					AccountGroupId = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_ID),
					AccountGroupName = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_NAME),
					AccountGroupDisplayValue = dataRow.ToString(DatabaseConstants.COL_ACCOUNT_GROUP_DISPLAY_VALUE),
					AccountGroupPosition = dataRow.ToInt(DatabaseConstants.COL_ACCOUNT_GROUP_POSITION),
					AccountGroupDisplayDefault = dataRow.ToBool(DatabaseConstants.COL_ACCOUNT_GROUP_DISPLAY_DEFAULT)
				};
		}

		public static AppUser CreateUser(DataRow dataRow)
		{
			return dataRow == null
				? null
				: new AppUser
				{
					Username = Ut.GetString(dataRow, DatabaseConstants.COL_USERNAME),
					Name = Ut.GetString(dataRow, DatabaseConstants.COL_NAME),
					UserId = Ut.GetGuid(dataRow, DatabaseConstants.COL_USER_ID),
					PrimaryEmail = Ut.GetString(dataRow, DatabaseConstants.COL_PRIMARY_EMAIL)
				};
		}

		public static LoginResult CreateResultLogin(DataRow dataRow, DataRow userDataRow)
		{
			if (dataRow == null)
				return null;
			var isAuthenticated = Ut.GetBool(dataRow, DatabaseConstants.COL_IS_AUTHENTICATED);
			var resetPassword = Ut.GetBool(dataRow, DatabaseConstants.COL_RESET_PASSWORD);
			var resultCode = Ut.GetString(dataRow, DatabaseConstants.COL_RESULT_CODE);
			var resultMessage = Ut.GetString(dataRow, DatabaseConstants.COL_RESULT_MESSAGE);
			var user = isAuthenticated ? CreateUser(userDataRow) : null;
			return new LoginResult
				{
					IsAuthenticated = isAuthenticated,
					ResetPassword = resetPassword,
					ResultCode = resultCode,
					ResultMessage = resultMessage,
					User = user
				};
		}

		public static Spend CreateSpend(DataRow dataRow)
		{
			return dataRow == null
				? null
				: new Spend
				{
					Id = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_ID),
					UserId = Ut.GetString(dataRow, DatabaseConstants.COL_USERNAME),
					Amount = Ut.GetFloat(dataRow, DatabaseConstants.COL_AMOUNT),
					Date = Ut.GetDateTime(dataRow, DatabaseConstants.COL_SPEND_DATE),
					SpendTypeId = Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_TYPE_ID),
					AccountId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_ID),
					AccountPeriodId = Ut.GetInt(dataRow, DatabaseConstants.COL_ACCOUNT_PERIOD_ID),
					Type = CreateSpendType(dataRow),
					AccountIdsArray = CreateArrayFromStringArray(dataRow, DatabaseConstants.COL_ACCOUNT_IDS),
					AccountIds =
						Ut.TrimStringList(Ut.GetString(dataRow, DatabaseConstants.COL_ACCOUNT_IDS), ','),
					Description = Ut.GetString(dataRow, DatabaseConstants.COL_DESCRIPTION),
					IsPending = dataRow.ToBool(DatabaseConstants.COL_IS_PENDING)
				};
		}

		private static FullAccountInfoViewModel.AccountType CreateAccountType(int accountType)
		{
			switch (accountType)
			{
				case 1: return FullAccountInfoViewModel.AccountType.Checking;
				case 2: return FullAccountInfoViewModel.AccountType.Saving;
				case 3: return FullAccountInfoViewModel.AccountType.Bank;
				default : return FullAccountInfoViewModel.AccountType.Undefined;
			}
		}

		private static int[] CreateArrayFromStringArray(DataRow dataRow, string columnName)
		{
			if (dataRow == null || string.IsNullOrWhiteSpace(columnName) || !dataRow.Table.Columns.Contains(columnName))
				return Array.Empty<int>();
			var value = Ut.GetString(dataRow, columnName);
			value = Ut.TrimStringList(value, ',');
			var arrayValue = value.Split(',');
			var list = new List<int>();
			foreach (var s in arrayValue)
			{
				int result;
				result = int.TryParse(s, out result) ? result : 0;
				if (result > 0)
				{
					list.Add(result);
				}
			}
			return list.ToArray();
		}

		public static SpendType CreateSpendType(DataRow dataRow)
		{
			SpendType spendType = dataRow == null
									  ? null
									  : new SpendType
										  {
											  Id =
												  Ut.GetInt(dataRow, DatabaseConstants.COL_SPEND_TYPE_ID),
											  Name =
												  Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_NAME),
											  Description =
												  Ut.GetString(dataRow, DatabaseConstants.COL_SPEND_TYPE_DESCRIPTION)
										  };
			return spendType;
		}

		public static List<SpendType> CreateSpendTypeList(DataTable dataTable)
		{
			return dataTable?.Rows.Cast<DataRow>().Select(CreateSpendType).ToList() ?? new List<SpendType>();
		}

		#region Internal Classes

		private class MainViewRow
		{
			public int AccountPeriodId { get; set; }
			public int AccountId { get; set; }
			public string AccountName { get; set; }
			public DateTime InitialDate { get; set; }
			public DateTime EndDate { get; set; }
			public int Position { get; set; }
			public int CurrentPeriodId { get; set; }
			public string HeaderColor { get; set; }
			public int CurrencyId { get; set; }
			public string CurrencyName { get; set; }
			public FullAccountInfoViewModel.AccountType Type{ get; set; }
			public int AccountGroupId { get; set; }
			public string AccountGroupName { get; set; }
			public string AccountGroupDisplayValue { get; set; }
			public int AccountGroupPosition { get; set; }
			public bool AccountGroupDisplayDefault { get; set; }
		}

		#endregion

		public static T GetEnum<T>(DataRow dataRow, string columnName) where T : struct, IConvertible
		{
			if (!typeof(T).IsEnum)
			{
				throw new ArgumentException("T must be an enumerated type");
			}

			var intValue = dataRow.ToInt(columnName);
			if (intValue == 0)
			{
				throw new ArgumentException($"Column {columnName} value is invalid");
			}

			var enumValue = (T)Enum.Parse(typeof(T), intValue.ToString());
			return enumValue;
		}

		private static int? GetNullableInt(DataRow dataRow, string columnName)
		{
			if (!dataRow.Table.Columns.Contains(columnName) || dataRow.IsDbNull(columnName))
			{
				return null;
			}

			var rowValue = dataRow[columnName];
			if(rowValue == null)
			{
				return null;
			}

			return dataRow.ToInt(columnName);
		}
	}
}
