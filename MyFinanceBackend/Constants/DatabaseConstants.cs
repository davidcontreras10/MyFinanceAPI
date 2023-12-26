namespace MyFinanceBackend.Constants
{
    public class DatabaseConstants
    {
        #region Columns

        public const string COL_LOAN_RECORD_ID = "LoanRecordId";
        public const string COL_LOAN_RECORD_NAME = "LoanRecordName";
        public const string COL_PAYMENT_SUMMARY = "PaymentSumary";

        public const string COL_IS_SUGGESTED = "IsSuggested";

        public const string COL_RESOURCE_ACTION_ID = "ResourceActionId";
		public const string COL_RESOURCE_ACTION_NAME = "ResourceActionName";
		public const string COL_APPLICATION_RESOURCE_ID = "ApplicationResourceId";
		public const string COL_APPLICATION_RESOURCE_NAME = "ApplicationResourceName";
		public const string COL_RESOURCE_ACCESS_LEVEL_ID = "ResourceAccessLevelId";
		public const string COL_RESOURCE_ACCESS_LEVEL_NAME = "ResourceAccessLevelName";
		public const string COL_APPLICATION_MODULE_ID = "ApplicationModuleId";
		public const string COL_APPLICATION_MODULE_NAME = "ApplicationModuleName";

		public const string COL_USERNAME = "Username";
        public const string COL_USER_ID = "UserId";
        public const string COL_PRIMARY_EMAIL = "PrimaryEmail";
        public const string COL_NAME = "Name";
        public const string COL_DESCRIPTION = "Description";
        public const string COL_TASK_DESCRIPTION = "TaskDescription";
        public const string COL_SPEND_DESCRIPTION = "SpendDescription";
        public const string COL_IS_AUTHENTICATED = "IsAuthenticated";
        public const string COL_RESET_PASSWORD = "ResultCode";
        public const string COL_RESULT_CODE = "ResultMessage";
        public const string COL_RESULT_MESSAGE = "ResultMessage";
        public const string COL_HAS_PERIODS = "HasPeriods";
        public const string COL_GENERAL_BALANCE = "GeneralBalance";
        public const string COL_ACCOUNT_GENERAL_BALANCE = "AccountGeneralBalance";
        public const string COL_ACCOUNT_GENERAL_BALANCE_TODAY = "AccountGeneralBalanceToday";
        public const string COL_ACCOUNT_PERIOD_BALANCE = "AccountPeriodBalance";
        public const string COL_ACCOUNT_PERIOD_SPENT = "AccountPeriodSpent";
        public const string COL_ACCOUNT_GLOBAL_ORDER = "GlobalOrder";
        public const string COL_IS_VALID = "IsValid";
        public const string COL_IS_SELECTED = "IsSelected";
        public const string COL_IS_SPEND_TRX = "IsSpendTrx";
        public const string COL_IS_PENDING = "IsPending";
        public const string COL_IS_LOAN = "IsLoan";
        public const string COL_IS_TRANSFER = "IsTransfer";
        public const string COL_IS_ORIGINAL = "IsOriginal";
        public const string COL_IS_DATE_VALID = "IsDateValid";
        public const string COL_ACTUAL_DATE = "ActualDate";
        public const string COL_INITIAL_DATE = "InitialDate";
        public const string COL_MAX_DATE = "MaxDate";
        public const string COL_MIN_DATE = "MinDate";
        public const string COL_END_DATE = "EndDate";
        public const string COL_SUGGESTED_DATE = "SuggestedDate";
        public const string COL_ACCOUNT_INCLUDE_IDS = "AccountIncludeIds";
        public const string COL_ACCOUNT_INCLUDE_ID = "AccountIncludeId";
        public const string COL_ACCOUNT_INCLUDE_NAME = "AccountIncludeName";
        public const string COL_BASE_BUDGET = "BaseBudget";
        public const string COL_HEADER_COLOR = "HeaderColor";
        public const string COL_ACCOUNT_HEADER_COLOR = "AccountHeaderColor";
        public const string COL_POSITION = "Position";
        public const string COL_ACCOUNT_POSITION = "AccountPosition";
        public const string COL_PERIOD_DEFINITION_ID = "PeriodDefinitionId";
        public const string COL_CUTTING_DATE = "CuttingDate";
        public const string COL_PERIOD_TYPE_NAME = "PeriodTypeName";
        public const string COL_PERIOD_TYPE_ID = "PeriodTypeId";
        public const string COL_ORIGINAL_AMOUNT = "OriginalAmount";
        public const string COL_ACCOUNT_TYPE = "AccountType";
        public const string COL_ACCOUNT_TYPE_ID = "AccountTypeId";
        public const string COL_ACCOUNT_TYPE_NAME = "AccountTypeName";
        public const string COL_FINANCIAL_ENTITY_ID = "FinancialEntityId";
        public const string COL_FINANCIAL_ENTITY_NAME = "FinancialEntityName";
        public const string COL_REPETITION = "Repetition";

        public const string COL_AMOUNT_TYPE = "AmountType";
        public const string COL_AMOUNT_TYPE_ID = "AmountTypeId";
        public const string COL_BUDGET = "Budget";
        public const string COL_AMOUNT = "Amount";
        public const string COL_CONVERTED_AMOUNT = "ConvertedAmount";
        public const string COL_AMOUNT_CURRENCY_ID = "AmountCurrencyId";
        public const string COL_SPEND_ID = "SpendId";
        public const string COL_SPEND_DATE = "SpendDate";
        public const string COL_SPEND_SET_PAYMENT_DATE = "SetPaymentDate";
        public const string COL_SPEND_AMOUNT = "SpendAmount";
        public const string COL_SPEND_CURRENCY_NAME = "SpendCurrencyName";
        public const string COL_SPEND_CURRENCY_SYMBOL = "SpendCurrencySymbol";
        public const string COL_SPEND_TYPE_ID = "SpendTypeId";
        public const string COL_SPEND_TYPE_NAME = "SpendTypeName";
        public const string COL_SPEND_TYPE_DESCRIPTION = "SpendTypeDescription";
        public const string COL_ACCOUNT_GROUP_ID = "AccountGroupId";
        public const string COL_ACCOUNT_GROUP_NAME = "AccountGroupName";
	    public const string COL_ACCOUNT_GROUP_DISPLAY_VALUE = "AccountGroupDisplayValue";
        public const string COL_ACCOUNT_GROUP_POSITION = "AccountGroupPosition";
	    public const string COL_ACCOUNT_GROUP_DISPLAY_DEFAULT = "AccountGroupDisplayDefault";
        public const string COL_ACCOUNT_ID = "AccountId";
        public const string COL_ACCOUNT_IDS = "AccountIds";
        public const string COL_ACCOUNT_NAME = "AccountName";
        public const string COL_TO_ACCOUNT_ID = "ToAccountId";
        public const string COL_TO_ACCOUNT_NAME = "ToAccountName";
        public const string COL_CURRENT_PERIOD_ID = "CurrentPeriodId";
        public const string COL_INCLUDE_DEFAULT = "IncludeDefault";
        public const string COL_ACCOUNT_PERIOD_ID = "AccountPeriodId";
        public const string COL_ACCOUNT_PERIOD_BUDGET = "AccountPeriodBudget";
        public const string COL_ACCOUNT_PERIOD_INITIAL_DATE = "AccountPeriodInitialDate";
        public const string COL_ACCOUNT_PERIOD_END_DATE = "AccountPeriodEndDate";

        public const string COL_CURRENCY_ID = "CurrencyId";
        public const string COL_CURRENCY_ID_ONE = "CurrencyIdOne";
        public const string COL_CURRENCY_ID_TWO = "CurrencyIdTwo";
        public const string COL_CURRENCY_NAME = "CurrencyName";
        public const string COL_CURRENCY_SYMBOL = "CurrencySymbol";

        public const string IS_SUCCESS = "IsSuccess";
        public const string COL_AFFECTED = "Affected";

        public const string COL_ACCOUNT_CURRENCY_ID = "AccountCurrencyId";
        public const string COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_ID = "CurrencyConverterMethodId";
        public const string COL_CONVERT_CURRENCY_ID = "ConvertCurrencyId";
        public const string COL_ACCOUNT_CURRENCY_CONVERTER_METHOD_NAME = "CurrencyConverterMethodName";
        public const string COL_ACCOUNT_CURRENCY_SYMBOL = "AccountCurrencySymbol";

        public const string COL_AMOUNT_NUMERATOR = "AmountNumerator";
        public const string COL_AMOUNT_DENOMINATOR = "AmountDenominator";
        public const string COL_NUMERATOR = "Numerator";
        public const string COL_DENOMINATOR = "Denominator";

        public const string COL_IS_DEFAULT = "IsDefault";
        public const string COL_IS_CURRENT_SELECTION = "IsCurrentSelection";

        public const string COL_DAYS = "Days";
        public const string COL_FREQ_TYPE = "PeriodTypeId";
        public const string COL_AUTOMATIC_TASK_ID = "AutomaticTaskId";

        public const string COL_LAST_EXECUTION_MSG = "ExecutionMsg";
        public const string COL_EXECUTION_MSG = "ExecutionMsg";
        public const string COL_LAST_EXECUTION_STATUS = "ExecutionStatus";
        public const string COL_EXECUTED_DATE = "ExecuteDatetime";
        public const string COL_EXECUTION_STATUS = "ExecutionStatus";

        #endregion

        #region Parameters

        public const string PAR_APPLICATION_RESOURCE_ID = "@pApplicationResourceId";
		public const string PAR_APPLICATION_MODULE_ID = "@pApplicationModuleId";
		public const string PAR_RESOURCE_ACTION_ID = "@pResourceActionId";
		public const string PAR_RESOURCE_ACCESS_LEVEL_ID = "@pResourceAccessLevelId";

		public const string PAR_CRITERIA_ID = "@pCriteriaId";

        public const string PAR_OUTPUT_MODIFIED = "@opModified";

        public const string PAR_NAME = "@pName";
        public const string PAR_PRIMARY_EMAIL = "@pPrimaryEmail";

        public const string PAR_IS_SELECTED = "@pIsSelected";

	    public const string PAR_INCLUDE_ALL = "@pIncludeAll";

		public const string PAR_ACCOUNT_GROUP_IDS = "@pAccountGroupIds";
        public const string PAR_ACCOUNT_GROUP_ID = "@pAccountGroupId";
        public const string PAR_ACCOUNT_GROUP_NAME = "@pAccountGroupName";
        public const string PAR_ACCOUNT_GROUP_DISPLAY_VALUE = "@pAccountGroupDisplayValue";
        public const string PAR_ACCOUNT_GROUP_POSITION = "@pAccountGroupPosition";
        public const string PAR_ACCOUNT_GROUP_DISPLAY_DEFAULT = "@pAccountGroupDisplayDefault";

	    public const string PAR_ACCOUNT_POSITIONS = "@pAccountPositions";

        public const string PAR_PENDING_SPENDS = "@pPendingSpends";
        public const string PAR_CURRENCY_DATATABLE = "@pCurrencyDataTable";
        public const string PAR_ACCOUNT_INCLUDE_UPDATE_TABLE = "@pAccountIncludeDataTable";

        public const string PAR_USER_ID = "@pUserId";
        public const string PAR_USERNAME = "@pUsername";
        public const string PAR_PASSWORD = "@pPassword";
        public const string PAR_CREATED_BY = "pCreatedByUserId";

        public const string PAR_IGNORE_ID_LIST = "@pIgnoreIdList";
        public const string PAR_SPEN_DEPENDENCY_ID = "@pDependencySpendId";
        public const string PAR_SPEND_ID = "@pSpendId";
        public const string PAR_SPEND_IDS = "@pSpendIds";
        public const string PAR_SPEND_TYPE = "@pSpendType";
        public const string PAR_SPEND_TYPE_ID = "@pSpendTypeId";
		public const string PAR_SPEND_TYPE_NAME = "@pSpendTypeName";
		public const string PAR_SPEND_TYPE_DESCRIPTION = "@pSpendTypeDescription";
        
        public const string PAR_MODIFY_LIST = "@pModifyList";
        public const string PAR_AMOUNT = "@pAmount";
        public const string PAR_AMOUNT_NUMERATOR = "@pAmountNumerator";
        public const string PAR_AMOUNT_DENOMINATOR = "@pAmountDenominator";
        public const string PAR_DATE_TIME = "@pDateTime";
        public const string PAR_DATE = "@pDate";
        public const string PAR_ACCOUNT_PERIOD_TABLE = "@pAccountPeriodTable";
        public const string PAR_ACCOUNT_PERIOD_IDS = "@pAccountPeriodIds";
        public const string PAR_ACCOUNT_IDS = "@pAccountIds";
        public const string PAR_ACCOUNT_ID = "@pAccountId";
	    public const string PAR_ACCOUNT_NAME = "@pAccountName";

	    public const string PAR_PERIOD_DEFINITION_ID = "@pPeriodDefinitionId";
	    public const string PAR_BASE_BUDGET = "@pBaseBudget";
	    public const string PAR_HEADER_COLOR = "@pHeaderColor";
	    public const string PAR_ACCOUNT_TYPE_ID = "@pAccountTypeId";
	    public const string PAR_FINANCIAL_ENTITY_ID = "@pFinancialEntityId";
	    public const string PAR_ACCOUNT_INCLUDE_DATA = "@pAccountIncludeData";
		public const string PAR_EDIT_FIELDS = "@pEditFields";

        public const string PAR_START_DATE = "@pStartDate";
        public const string PAR_END_DATE = "@pEndDate";

        public const string PAR_BUDGET = "@pBudget";
        public const string PAR_DESCRIPTION = "@pDescription";
        public const string PAR_SPEND_DESCRIPTION = "@pSpendDescription";
        public const string PAR_SPEND_DATE = "@pSpendDate";
        public const string PAR_SPEND_SET_PAYMENT_DATE = "@pSetPaymentDate";

        public const string PAR_ACCOUNTS_TABLE = "@pAccountsTable";

        public const string PAR_CURRENCY_ID = "@pCurrencyId";
        public const string PAR_AMOUNT_CURRENCY_ID = "@pAmountCurrencyId";
        public const string PAR_DESTINATION_CURRENCY_ID = "@pDestinationCurrencyId";

        public const string PAR_ACCOUNT_PERIOD_ID = "@pAccountPeriodId";

        public const string PAR_AMOUNT_TYPE_NAME = "@pAmountTypeName";

        public const string PAR_IS_PENDING = "@pIsPending";

        public const string PAR_LOAN_NAME = "@pLoanName";
        public const string PAR_LOAN_RECORD_ID = "@pLoanRecordId";
        public const string PAR_LOAN_RECORD_STATUS_ID = "@pLoanRecordStatusId";
        public const string PAR_LOAN_RECORD_IDS = "@pLoanRecordIds";

        public const string PAR_TASK_DESC = "@pTaskDescription";
        public const string PAR_TO_ACCOUNT_ID = "@pToAccount";
        public const string PAR_PERIOD_TYPE_ID = "@pPeriodTypeId";
        public const string PAR_DAYS = "@pDays";
        public const string PAR_IS_SPEND_TRX = "@pIsSpendTrx";

        public const string PAR_AUTOMATIC_TASK_ID = "@pAutomaticTaskId";
        public const string PAR_EXECUTED_DATETIME = "@pExecutedDatetime";
        public const string PAR_EXECUTED_STATUS = "@pExecutedStatus";
        public const string PAR_EXECUTED_MSG = "@pExecutionMsg";

        #endregion

        #region Stored Procedures

        public const string SP_RESOURCE_ACCESS_REPORT = "SpResourceAccessList";
		public const string SP_SPEND_ATTRIBUTES_LIST = "SpSpendAttributesList";
        public const string SP_LOAN_IDS_LIST = "SpLoanIdList";
        public const string SP_LOAN_DETAIL_BY_IDS = "SpLoanRecordDetailList";
        public const string SP_ACCOUNT_PERIOD_BY_LOAN_DATE = "SpAccountPeriodByDateLoan";
        public const string SP_SPEND_DEPENDENCY_ADD = "SpSpendDependencyAdd";
        public const string SP_LOAN_SPEND_ADD = "SpLoanSpendAdd";
        public const string SP_LOAN_CLOSE = "SpLoanClose";
        public const string SP_LOAN_DELETE = "SpLoanDelete";
        public const string SP_LOAN_ADD_EDIT = "SpLoanAddEdit";
        public const string SP_USER_ASSIGNED_ACCESS_LIST = "SpUserAssignedAccessList";
        public const string SP_USERS_EDIT = "SpUsersEdit";
        public const string SP_USERS_ADD = "SpUsersAdd";
        public const string SP_ACCOUNT_GROUP_DELETE = "SpAccountGroupDelete";
        public const string SP_ACCOUNT_GROUP_ADD_EDIT = "SpAccountGroupAddEdit";
	    public const string SP_ACCOUNT_GROUP_DETAIL = "SpAccountGroupDetail";
        public const string SP_ACCOUNT_DELETE = "SpAccountDelete";
	    public const string SP_ACCOUNT_EDIT = "SpAccountEdit";
        public const string SP_ACCOUNT_ADD = "SpAccountAdd";
	    public const string SP_ACCOUNT_POSITION_UPDATE = "SpAccountPositionUpdate";
        public const string SP_TRANSFER_RECORD_ADD = "SpTransferRecordAdd";
        public const string SP_ACCOUNT_DEFAULT_CURRENCY_VALUES_LIST = "SpAccountDefaultCurrencyValuesList";
        public const string SP_TRANSFER_ACCOUNT_DEFAULT_CURRENCY_VALUES_LIST = "SpTransferAccountDefaultCurrencyValuesList";
        public const string SP_TRANSFER_ACCOUNT_INFO = "SpTransfersAccountInfo";
        public const string SP_TRANSFER_POSSIBLE_DESTINATION_ACCOUNTS = "SpTransfersPossibleDestinationAccountList";
        public const string SP_SPEND_POSSIBLE_CURRENCIES = "SpAccountPossibleSpendCurrenciesList";
        public const string SP_USER_LIST = "SpUsersList";
        public const string SP_USERS_OWNED_LIST = "SpUsersOwnedList";
        public const string SP_LOGIN_ATTEMPT = "SpLoginAttempt";
	    public const string SP_USER_SET_PASSWORD = "SpUserSetPassword";
        public const string SP_SPEND_ADD = "SpSpendAdd";
        public const string SP_SPENDS_SAVED_LIST = "SpSpendsClientList";
        public const string SP_SPEND_ADD_BY_ACCOUNT = "SpSpendByAccountsAdd";
        public const string SP_SPEND_EDIT_BY_ACCOUNT = "SpSpendByAccountsEdit";
        public const string SP_SPEND_DELETE = "SpSpendsDelete";
        public const string SP_SPEND_LIST = "SpSpendsList";
        public const string SP_SPEND_BY_PERIOD_LIST = "SpSpendsByPeriodList";
		public const string SP_SPEND_TYPE_BY_ACCOUNT_LIST = "SpSpendTypeByAccountList";
		public const string SP_SPEND_TYPE_ADD_EDIT = "SpSpendTypeAddEdit";
	    public const string SP_SPEND_TYPE_USER_ADD = "SpSpendTypeUserAdd";
		public const string SP_SPEND_TYPE_USER_DELETE = "SpSpendTypeUserDelete";
		public const string SP_SPEND_TYPE_LIST = "SpSpendTypeList";
        public const string SP_SPEND_TYPE_DELETE = "SpSpendTypeDelete";
        public const string SP_SPEND_DETAIL_LIST = "SpSpendDetailList";
        public const string SP_MAIN_VIEW_DATA = "SpMainViewList";
        public const string SP_PERIOD_CREATE = "SpPeriodCreate";
        public const string SP_ACCOUNT_PERIOD_NEXT_VALUES = "SpAccountPeriodNextValues";
        public const string SP_DATE_RANGE_ACCOUNTS = "SpDateRangeByAccounts";
        public const string SP_SPEND_EDIT = "SpSpendsEdit";
        public const string SP_ACCOUNTS_CURRENCIES_LIST = "SpAccountsCurrenciesList";
        public const string SP_FINANCE_SPEND_BY_ACCOUNT_LIST = "SpFinanceSpendByAccountsList";
        public const string SP_ADD_SPEND_VIEW_MODEL_LIST = "SpAddSpendViewModelList";
        public const string SP_EDIT_SPEND_VIEW_MODEL_LIST = "SpEditSpendViewModelList";
        public const string SP_ADD_SPEND_CURRENCY_VALIDATE = "SpAddSpendCurrencyValidate";
        public const string SP_ADD_SPEND_ACCOUNT_INCLUDE_LIST = "SpAddSpendAccountIncludeList";
        public const string SP_ACCOUNTS_DETAILS_LIST = "SpAccountsDetailList";
        public const string SP_POSSIBLE_ACCOUNT_INCLUDE_LIST = "SpPossibleAccountIncludeList";
        public const string SP_ACCOUNT_LIST = "SpAccountList";
        public const string SP_ACCOUNT_W_PERIOD_LIST = "SpAccountWithPeriodList";
        public const string SP_ACCOUNTS_CREATE_VIEW_MODEL = "SpAccountsCreateViewModel";
        public const string SP_ACCOUNT_ORDER_LIST = "SpAccountOrderList";
        public const string SP_BASIC_ACCOUNT_PERIOD_LIST = "SpBasicAccountPeriodList";
        public const string SP_BASIC_ACCOUNT_PERIOD_ID_DATE = "SpAccountPeriodByAccountIdDateTimeList";
        public const string SP_BASIC_ACCOUNT_BY_ID = "SpBasicAccountList";
	    public const string SP_USER_BANK_SUMMARY_ACCOUNT_LIST = "SpUserBankSummaryAccountList";
	    public const string SP_USER_BANK_SUMMARY_ACCOUNT_PERIOD_LIST = "SpUserBankSummaryAccountPeriodList";

	    public const string SP_AUTO_TASK_BASIC_INSERT = "SpAutoTrxBasicInsert";
        public const string SP_AUTO_TASK_TRANSFER_INSERT = "SpAutoTrxTransferInsert";
        public const string SP_AUTO_TASK_BY_PARAM_LIST = "SpAutoTrxList";

        public const string SP_EXECUTED_TASKS_LIST = "SpExecutedTaskList";
        public const string SP_AUTO_TASK_DELETE = "SpAutoTrxDelete";
        public const string SP_EXECUTED_TASK_INSERT = "SpExecutedTrxInsert";

        #endregion
    }
}