using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EFDataAccess.Migrations
{
    public partial class InitialCreate : Migration
    {
		protected override void Up(MigrationBuilder migrationBuilder)
        {

        }


		protected void UpForEmpty(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountType",
                columns: table => new
                {
                    AccountTypeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountTypeName = table.Column<string>(unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountType", x => x.AccountTypeId);
                });

            migrationBuilder.CreateTable(
                name: "AmountType",
                columns: table => new
                {
                    AmountTypeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AmountTypeName = table.Column<string>(maxLength: 100, nullable: false),
                    AmountSign = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmountType", x => x.AmountTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationModule",
                columns: table => new
                {
                    ApplicationModuleId = table.Column<int>(nullable: false),
                    ApplicationModuleName = table.Column<string>(unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationModule", x => x.ApplicationModuleId);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationResource",
                columns: table => new
                {
                    ApplicationResourceId = table.Column<int>(nullable: false),
                    ApplicationResourceName = table.Column<string>(maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationResource", x => x.ApplicationResourceId);
                });

            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false, defaultValueSql: "(newid())"),
                    Username = table.Column<string>(maxLength: 100, nullable: false),
                    Name = table.Column<string>(maxLength: 500, nullable: false),
                    Password = table.Column<string>(maxLength: 500, nullable: false),
                    PrimaryEmail = table.Column<string>(unicode: false, maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                    table.ForeignKey(
                        name: "AppUser_FK_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BccrWebServiceIndicator",
                columns: table => new
                {
                    EntityName = table.Column<string>(unicode: false, maxLength: 500, nullable: false),
                    SellCode = table.Column<string>(unicode: false, maxLength: 50, nullable: false),
                    PurchaseCode = table.Column<string>(unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BccrWebServiceIndicator", x => x.EntityName);
                });

            migrationBuilder.CreateTable(
                name: "Currency",
                columns: table => new
                {
                    CurrencyId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 500, nullable: false),
                    Symbol = table.Column<string>(maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currency", x => x.CurrencyId);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyConverter",
                columns: table => new
                {
                    CurrencyConverterId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyIdOne = table.Column<int>(nullable: false),
                    CurrencyIdTwo = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyConverter", x => x.CurrencyConverterId);
                });

            migrationBuilder.CreateTable(
                name: "DailyJob",
                columns: table => new
                {
                    DailyJobId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EventDesc = table.Column<string>(unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyJob", x => x.DailyJobId);
                });

            migrationBuilder.CreateTable(
                name: "EntitiesSupported",
                columns: table => new
                {
                    EntitiesSupportedId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(unicode: false, maxLength: 500, nullable: true),
                    EntitySearchKey = table.Column<string>(unicode: false, maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitiesSupported", x => x.EntitiesSupportedId);
                });

            migrationBuilder.CreateTable(
                name: "FinancialEntity",
                columns: table => new
                {
                    FinancialEntityId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialEntity", x => x.FinancialEntityId);
                });

            migrationBuilder.CreateTable(
                name: "LoanRecordStatus",
                columns: table => new
                {
                    LoanRecordStatusId = table.Column<int>(nullable: false),
                    LoanRecordStatusName = table.Column<string>(unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRecordStatus", x => x.LoanRecordStatusId);
                });

            migrationBuilder.CreateTable(
                name: "PeriodType",
                columns: table => new
                {
                    PeriodTypeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodType", x => x.PeriodTypeId);
                });

            migrationBuilder.CreateTable(
                name: "ResourceAccessLevel",
                columns: table => new
                {
                    ResourceAccessLevelId = table.Column<int>(nullable: false),
                    ResourceAccessLevelName = table.Column<string>(maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAccessLevel", x => x.ResourceAccessLevelId);
                });

            migrationBuilder.CreateTable(
                name: "ResourceAction",
                columns: table => new
                {
                    ResourceActionId = table.Column<int>(nullable: false),
                    ResourceActionName = table.Column<string>(maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAction", x => x.ResourceActionId);
                });

            migrationBuilder.CreateTable(
                name: "SpendType",
                columns: table => new
                {
                    SpendTypeId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 500, nullable: false),
                    Description = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendType", x => x.SpendTypeId);
                });

            migrationBuilder.CreateTable(
                name: "SpFinanceSpendByAccountsListTable",
                columns: table => new
                {
                    AccountId = table.Column<int>(nullable: true),
                    AccountPeriodId = table.Column<int>(nullable: true),
                    AccountCurrencyId = table.Column<int>(nullable: true),
                    AccountCurrencySymbol = table.Column<string>(unicode: false, maxLength: 1, nullable: true),
                    AccountGeneralBalance = table.Column<double>(nullable: true),
                    AccountGeneralBalanceToday = table.Column<double>(nullable: true),
                    AccountPeriodBalance = table.Column<double>(nullable: true),
                    AccountPeriodSpent = table.Column<double>(nullable: true),
                    InitialDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    Budget = table.Column<double>(nullable: true),
                    SpendId = table.Column<int>(nullable: true),
                    SpendAmount = table.Column<double>(nullable: true),
                    Numerator = table.Column<double>(nullable: true),
                    Denominator = table.Column<double>(nullable: true),
                    SpendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    SpendTypeName = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    SpendCurrencyName = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    SpendCurrencySymbol = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    AmountType = table.Column<int>(nullable: true),
                    AccountName = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    IsPending = table.Column<bool>(nullable: true, defaultValueSql: "((0))"),
                    SetPaymentDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    IsValid = table.Column<bool>(nullable: true),
                    IsLoan = table.Column<bool>(nullable: true),
                    SpendDescription = table.Column<string>(maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "AccountGroup",
                columns: table => new
                {
                    AccountGroupId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountGroupName = table.Column<string>(unicode: false, maxLength: 500, nullable: false),
                    DisplayValue = table.Column<string>(unicode: false, maxLength: 500, nullable: true),
                    AccountGroupPosition = table.Column<int>(nullable: true),
                    DisplayDefault = table.Column<bool>(nullable: true),
                    UserId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountGroup", x => x.AccountGroupId);
                    table.ForeignKey(
                        name: "AccountGroup_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppUserOwner",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    OwnerUserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "AppUserOwner_FK_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AppUserOwner_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyConverterMethod",
                columns: table => new
                {
                    CurrencyConverterMethodId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CurrencyConverterId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 500, nullable: false),
                    IsDefault = table.Column<bool>(nullable: true),
                    FinancialEntityId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyConverterMethod", x => x.CurrencyConverterMethodId);
                    table.ForeignKey(
                        name: "CurrencyConverterMethod_FK_CurrencyIdOne",
                        column: x => x.CurrencyConverterId,
                        principalTable: "CurrencyConverter",
                        principalColumn: "CurrencyConverterId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MethodsSupported",
                columns: table => new
                {
                    EntitiesSupportedId = table.Column<int>(nullable: false),
                    MethodId = table.Column<int>(nullable: false),
                    Colones = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "MethodsSupported_FK_EntitiesSupportedId",
                        column: x => x.EntitiesSupportedId,
                        principalTable: "EntitiesSupported",
                        principalColumn: "EntitiesSupportedId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PeriodDefinition",
                columns: table => new
                {
                    PeriodDefinitionId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodTypeId = table.Column<int>(nullable: false),
                    CuttingDate = table.Column<string>(maxLength: 500, nullable: true),
                    Repetition = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodDefinition", x => x.PeriodDefinitionId);
                    table.ForeignKey(
                        name: "PeriodDefinition_FK_PeriodTypeId",
                        column: x => x.PeriodTypeId,
                        principalTable: "PeriodType",
                        principalColumn: "PeriodTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequiredAccess",
                columns: table => new
                {
                    ResourceActionId = table.Column<int>(nullable: false),
                    ApplicationResourceId = table.Column<int>(nullable: false),
                    ResourceAccessLevelId = table.Column<int>(nullable: false),
                    ApplicationModuleId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "ResourceRequiredAccess_FK_ApplicationModuleId",
                        column: x => x.ApplicationModuleId,
                        principalTable: "ApplicationModule",
                        principalColumn: "ApplicationModuleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ResourceRequiredAccess_FK_ApplicationResourceId",
                        column: x => x.ApplicationResourceId,
                        principalTable: "ApplicationResource",
                        principalColumn: "ApplicationResourceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ResourceRequiredAccess_FK_ResourceAccessLevelId",
                        column: x => x.ResourceAccessLevelId,
                        principalTable: "ResourceAccessLevel",
                        principalColumn: "ResourceAccessLevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ResourceRequiredAccess_FK_ResourceActionId",
                        column: x => x.ResourceActionId,
                        principalTable: "ResourceAction",
                        principalColumn: "ResourceActionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAssignedAccess",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    ResourceActionId = table.Column<int>(nullable: false),
                    ApplicationResourceId = table.Column<int>(nullable: false),
                    ResourceAccessLevelId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "UserAssignedAccess_FK_ApplicationResourceId",
                        column: x => x.ApplicationResourceId,
                        principalTable: "ApplicationResource",
                        principalColumn: "ApplicationResourceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UserAssignedAccess_FK_ResourceAccessLevelId",
                        column: x => x.ResourceAccessLevelId,
                        principalTable: "ResourceAccessLevel",
                        principalColumn: "ResourceAccessLevelId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UserAssignedAccess_FK_ResourceActionId",
                        column: x => x.ResourceActionId,
                        principalTable: "ResourceAction",
                        principalColumn: "ResourceActionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UserAssignedAccess_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Spend",
                columns: table => new
                {
                    SpendId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalAmount = table.Column<double>(nullable: true),
                    SpendDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    SpendTypeId = table.Column<int>(nullable: true),
                    Description = table.Column<string>(maxLength: 500, nullable: false),
                    AmountCurrencyId = table.Column<int>(nullable: true),
                    AmountTypeId = table.Column<int>(nullable: false),
                    Numerator = table.Column<double>(nullable: true, defaultValueSql: "((1))"),
                    Denominator = table.Column<double>(nullable: true, defaultValueSql: "((1))"),
                    IsPending = table.Column<bool>(nullable: false),
                    SetPaymentDate = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spend", x => x.SpendId);
                    table.ForeignKey(
                        name: "Spend_FK_AmountCurrencyId",
                        column: x => x.AmountCurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Spend_FK_AmountTypeId",
                        column: x => x.AmountTypeId,
                        principalTable: "AmountType",
                        principalColumn: "AmountTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Spend_FK_SpendTypeId",
                        column: x => x.SpendTypeId,
                        principalTable: "SpendType",
                        principalColumn: "SpendTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSpendType",
                columns: table => new
                {
                    UserId = table.Column<Guid>(nullable: false),
                    SpendTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSpendType", x => new { x.UserId, x.SpendTypeId });
                    table.ForeignKey(
                        name: "UserSpendType_FK_SpendTypeId",
                        column: x => x.SpendTypeId,
                        principalTable: "SpendType",
                        principalColumn: "SpendTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UserSpendType_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Account",
                columns: table => new
                {
                    AccountId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodDefinitionId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 500, nullable: false),
                    CurrencyId = table.Column<int>(nullable: true),
                    BaseBudget = table.Column<double>(nullable: true),
                    Position = table.Column<int>(nullable: true),
                    HeaderColor = table.Column<string>(maxLength: 500, nullable: true),
                    AccountTypeId = table.Column<int>(nullable: false, defaultValueSql: "((1))"),
                    DefaultSpendTypeId = table.Column<int>(nullable: true),
                    FinancialEntityId = table.Column<int>(nullable: true),
                    UserId = table.Column<Guid>(nullable: true),
                    AccountGroupId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Account", x => x.AccountId);
                    table.ForeignKey(
                        name: "Account_FK_AccountGroupId",
                        column: x => x.AccountGroupId,
                        principalTable: "AccountGroup",
                        principalColumn: "AccountGroupId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "AccountType",
                        principalColumn: "AccountTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_DefaultSpendTypeId",
                        column: x => x.DefaultSpendTypeId,
                        principalTable: "SpendType",
                        principalColumn: "SpendTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_FinancialEntityId",
                        column: x => x.FinancialEntityId,
                        principalTable: "FinancialEntity",
                        principalColumn: "FinancialEntityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_PeriodDefinitionId",
                        column: x => x.PeriodDefinitionId,
                        principalTable: "PeriodDefinition",
                        principalColumn: "PeriodDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "Account_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanRecord",
                columns: table => new
                {
                    LoanRecordId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoanRecordName = table.Column<string>(unicode: false, maxLength: 100, nullable: true),
                    SpendId = table.Column<int>(nullable: false),
                    LoanRecordStatusId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRecord", x => x.LoanRecordId);
                    table.ForeignKey(
                        name: "LoanRecord_FK_LoanRecordStatusId",
                        column: x => x.LoanRecordStatusId,
                        principalTable: "LoanRecordStatus",
                        principalColumn: "LoanRecordStatusId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "LoanRecord_FK_SpendId",
                        column: x => x.SpendId,
                        principalTable: "Spend",
                        principalColumn: "SpendId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpendDependencies",
                columns: table => new
                {
                    SpendId = table.Column<int>(nullable: false),
                    DependencySpendId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "SpendDependencies_FK_SpendId",
                        column: x => x.SpendId,
                        principalTable: "Spend",
                        principalColumn: "SpendId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRecord",
                columns: table => new
                {
                    TransferRecordId = table.Column<int>(nullable: false),
                    SpendId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "TransferRecord_FK_SpendId",
                        column: x => x.SpendId,
                        principalTable: "Spend",
                        principalColumn: "SpendId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountInclude",
                columns: table => new
                {
                    AccountId = table.Column<int>(nullable: false),
                    AccountIncludeId = table.Column<int>(nullable: false),
                    CurrencyConverterMethodId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInclude", x => new { x.AccountId, x.AccountIncludeId });
                    table.ForeignKey(
                        name: "AccountInclude_FK_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AccountInclude_FK_AccountIncludeId",
                        column: x => x.AccountIncludeId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AccountInclude_FK_CurrencyConverterMethodId",
                        column: x => x.CurrencyConverterMethodId,
                        principalTable: "CurrencyConverterMethod",
                        principalColumn: "CurrencyConverterMethodId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountPeriod",
                columns: table => new
                {
                    AccountPeriodId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<int>(nullable: true),
                    Budget = table.Column<double>(nullable: true),
                    InitialDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CurrencyId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPeriod", x => x.AccountPeriodId);
                    table.ForeignKey(
                        name: "AccountPeriod_FK_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AccountPeriod_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AutomaticTask",
                columns: table => new
                {
                    AutomaticTaskId = table.Column<Guid>(nullable: false),
                    Amount = table.Column<double>(nullable: false),
                    SpendTypeId = table.Column<int>(nullable: false),
                    CurrencyId = table.Column<int>(nullable: false),
                    TaskDescription = table.Column<string>(maxLength: 400, nullable: true),
                    AccountId = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false),
                    PeriodTypeId = table.Column<int>(nullable: false),
                    Days = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomaticTask", x => x.AutomaticTaskId);
                    table.ForeignKey(
                        name: "AutomaticTask_FK_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AutomaticTask_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AutomaticTask_FK_SpendTypeId",
                        column: x => x.SpendTypeId,
                        principalTable: "SpendType",
                        principalColumn: "SpendTypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "AutomaticTask_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserBankSummaryAccount",
                columns: table => new
                {
                    AccountId = table.Column<int>(nullable: false),
                    UserId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBankSummaryAccount", x => new { x.AccountId, x.UserId });
                    table.ForeignKey(
                        name: "UserBankSummaryAccount_FK_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "UserBankSummaryAccount_FK_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanSpend",
                columns: table => new
                {
                    LoanRecordId = table.Column<int>(nullable: false),
                    SpendId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "LoanSpend_FK_LoanRecordId",
                        column: x => x.LoanRecordId,
                        principalTable: "LoanRecord",
                        principalColumn: "LoanRecordId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "LoanSpend_FK_SpendId",
                        column: x => x.SpendId,
                        principalTable: "Spend",
                        principalColumn: "SpendId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpendOnPeriod",
                columns: table => new
                {
                    SpendId = table.Column<int>(nullable: false),
                    AccountPeriodId = table.Column<int>(nullable: false),
                    Numerator = table.Column<double>(nullable: true),
                    Denominator = table.Column<double>(nullable: true),
                    PendingUpdate = table.Column<bool>(nullable: true),
                    CurrencyConverterMethodId = table.Column<int>(nullable: true),
                    IsOriginal = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpendOnPeriod", x => new { x.SpendId, x.AccountPeriodId });
                    table.ForeignKey(
                        name: "SpendOnPeriod_FK_AccountPeriodId",
                        column: x => x.AccountPeriodId,
                        principalTable: "AccountPeriod",
                        principalColumn: "AccountPeriodId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "SpendOnPeriod_FK_CurrencyConverterMethodId",
                        column: x => x.CurrencyConverterMethodId,
                        principalTable: "CurrencyConverterMethod",
                        principalColumn: "CurrencyConverterMethodId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "SpendOnPeriod_FK_SpendId",
                        column: x => x.SpendId,
                        principalTable: "Spend",
                        principalColumn: "SpendId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutedTask",
                columns: table => new
                {
                    ExecutedTaskId = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AutomaticTaskId = table.Column<Guid>(nullable: false),
                    ExecutedByUserId = table.Column<Guid>(nullable: false),
                    ExecuteDatetime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ExecutionStatus = table.Column<int>(nullable: false),
                    ExecutionMsg = table.Column<string>(unicode: false, maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutedTask", x => x.ExecutedTaskId);
                    table.ForeignKey(
                        name: "ExecutedTask_FK_AutomaticTaskId",
                        column: x => x.AutomaticTaskId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "ExecutedTask_FK_UserId",
                        column: x => x.ExecutedByUserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpInTrxDef",
                columns: table => new
                {
                    SpInTrxDefId = table.Column<Guid>(nullable: false),
                    IsSpendTrx = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpInTrxDef", x => x.SpInTrxDefId);
                    table.ForeignKey(
                        name: "SpInTrxDef_FK_AutomaticTaskId",
                        column: x => x.SpInTrxDefId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferTrxDef",
                columns: table => new
                {
                    TransferTrxDefId = table.Column<Guid>(nullable: false),
                    ToAccountId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferTrxDef", x => x.TransferTrxDefId);
                    table.ForeignKey(
                        name: "TransferTrxDef_FK_ToAccountId",
                        column: x => x.ToAccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "TransferTrxDef_FK_AutomaticTaskId",
                        column: x => x.TransferTrxDefId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Account_AccountGroupId",
                table: "Account",
                column: "AccountGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_AccountTypeId",
                table: "Account",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_CurrencyId",
                table: "Account",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_DefaultSpendTypeId",
                table: "Account",
                column: "DefaultSpendTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_FinancialEntityId",
                table: "Account",
                column: "FinancialEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_PeriodDefinitionId",
                table: "Account",
                column: "PeriodDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_Account_UserId",
                table: "Account",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_UserId",
                table: "AccountGroup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInclude_AccountIncludeId",
                table: "AccountInclude",
                column: "AccountIncludeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInclude_CurrencyConverterMethodId",
                table: "AccountInclude",
                column: "CurrencyConverterMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPeriod_AccountId",
                table: "AccountPeriod",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPeriod_CurrencyId",
                table: "AccountPeriod",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "AmountTypeName_unique",
                table: "AmountType",
                column: "AmountTypeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUser_CreatedByUserId",
                table: "AppUser",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "AppUser_Unq_Username",
                table: "AppUser",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserOwner_OwnerUserId",
                table: "AppUserOwner",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "ClusteredIndex-20210912-193500",
                table: "AppUserOwner",
                columns: new[] { "UserId", "OwnerUserId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomaticTask_AccountId",
                table: "AutomaticTask",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "AutomaticTask_Unq_AutomaticTaskId",
                table: "AutomaticTask",
                column: "AutomaticTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutomaticTask_CurrencyId",
                table: "AutomaticTask",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomaticTask_SpendTypeId",
                table: "AutomaticTask",
                column: "SpendTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomaticTask_UserId",
                table: "AutomaticTask",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyConverterMethod_CurrencyConverterId",
                table: "CurrencyConverterMethod",
                column: "CurrencyConverterId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutedTask_AutomaticTaskId",
                table: "ExecutedTask",
                column: "AutomaticTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutedTask_ExecutedByUserId",
                table: "ExecutedTask",
                column: "ExecutedByUserId");

            migrationBuilder.CreateIndex(
                name: "ExecutedTask_Unq_ExecutedTaskId",
                table: "ExecutedTask",
                column: "ExecutedTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanRecord_LoanRecordStatusId",
                table: "LoanRecord",
                column: "LoanRecordStatusId");

            migrationBuilder.CreateIndex(
                name: "LoanRecord_UQ_SpendId",
                table: "LoanRecord",
                column: "SpendId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanSpend_SpendId",
                table: "LoanSpend",
                column: "SpendId");

            migrationBuilder.CreateIndex(
                name: "PK_LoanSpend",
                table: "LoanSpend",
                columns: new[] { "LoanRecordId", "SpendId" },
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_MethodsSupported_EntitiesSupportedId",
                table: "MethodsSupported",
                column: "EntitiesSupportedId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodDefinition_PeriodTypeId",
                table: "PeriodDefinition",
                column: "PeriodTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequiredAccess_ApplicationModuleId",
                table: "ResourceRequiredAccess",
                column: "ApplicationModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequiredAccess_ApplicationResourceId",
                table: "ResourceRequiredAccess",
                column: "ApplicationResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequiredAccess_ResourceAccessLevelId",
                table: "ResourceRequiredAccess",
                column: "ResourceAccessLevelId");

            migrationBuilder.CreateIndex(
                name: "ClusteredIndex-20210912-193313",
                table: "ResourceRequiredAccess",
                columns: new[] { "ResourceActionId", "ApplicationResourceId", "ResourceAccessLevelId", "ApplicationModuleId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_Spend_AmountCurrencyId",
                table: "Spend",
                column: "AmountCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Spend_AmountTypeId",
                table: "Spend",
                column: "AmountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Spend_SpendTypeId",
                table: "Spend",
                column: "SpendTypeId");

            migrationBuilder.CreateIndex(
                name: "PK_SpendDependencies",
                table: "SpendDependencies",
                columns: new[] { "SpendId", "DependencySpendId" },
                unique: true)
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_SpendOnPeriod_AccountPeriodId",
                table: "SpendOnPeriod",
                column: "AccountPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_SpendOnPeriod_CurrencyConverterMethodId",
                table: "SpendOnPeriod",
                column: "CurrencyConverterMethodId");

            migrationBuilder.CreateIndex(
                name: "ClusteredIndex-20210912-192807",
                table: "SpFinanceSpendByAccountsListTable",
                columns: new[] { "AccountId", "AccountPeriodId", "SpendId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "SpInTrxDef_Unq_SpInTrxDefId",
                table: "SpInTrxDef",
                column: "SpInTrxDefId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferRecord_SpendId",
                table: "TransferRecord",
                column: "SpendId");

            migrationBuilder.CreateIndex(
                name: "ClusteredIndex-20210912-192222",
                table: "TransferRecord",
                columns: new[] { "TransferRecordId", "SpendId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_TransferTrxDef_ToAccountId",
                table: "TransferTrxDef",
                column: "ToAccountId");

            migrationBuilder.CreateIndex(
                name: "TransferTrxDef_Unq_TransferTrxDefId",
                table: "TransferTrxDef",
                column: "TransferTrxDefId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignedAccess_ApplicationResourceId",
                table: "UserAssignedAccess",
                column: "ApplicationResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignedAccess_ResourceAccessLevelId",
                table: "UserAssignedAccess",
                column: "ResourceAccessLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAssignedAccess_ResourceActionId",
                table: "UserAssignedAccess",
                column: "ResourceActionId");

            migrationBuilder.CreateIndex(
                name: "ClusteredIndex-20210912-193345",
                table: "UserAssignedAccess",
                columns: new[] { "UserId", "ResourceActionId", "ApplicationResourceId", "ResourceAccessLevelId" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBankSummaryAccount_UserId",
                table: "UserBankSummaryAccount",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSpendType_SpendTypeId",
                table: "UserSpendType",
                column: "SpendTypeId");
        }

		protected override void Down(MigrationBuilder migrationBuilder)
        {

        }


		protected void DownForEmpty(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountInclude");

            migrationBuilder.DropTable(
                name: "AppUserOwner");

            migrationBuilder.DropTable(
                name: "BccrWebServiceIndicator");

            migrationBuilder.DropTable(
                name: "DailyJob");

            migrationBuilder.DropTable(
                name: "ExecutedTask");

            migrationBuilder.DropTable(
                name: "LoanSpend");

            migrationBuilder.DropTable(
                name: "MethodsSupported");

            migrationBuilder.DropTable(
                name: "ResourceRequiredAccess");

            migrationBuilder.DropTable(
                name: "SpendDependencies");

            migrationBuilder.DropTable(
                name: "SpendOnPeriod");

            migrationBuilder.DropTable(
                name: "SpFinanceSpendByAccountsListTable");

            migrationBuilder.DropTable(
                name: "SpInTrxDef");

            migrationBuilder.DropTable(
                name: "TransferRecord");

            migrationBuilder.DropTable(
                name: "TransferTrxDef");

            migrationBuilder.DropTable(
                name: "UserAssignedAccess");

            migrationBuilder.DropTable(
                name: "UserBankSummaryAccount");

            migrationBuilder.DropTable(
                name: "UserSpendType");

            migrationBuilder.DropTable(
                name: "LoanRecord");

            migrationBuilder.DropTable(
                name: "EntitiesSupported");

            migrationBuilder.DropTable(
                name: "ApplicationModule");

            migrationBuilder.DropTable(
                name: "AccountPeriod");

            migrationBuilder.DropTable(
                name: "CurrencyConverterMethod");

            migrationBuilder.DropTable(
                name: "AutomaticTask");

            migrationBuilder.DropTable(
                name: "ApplicationResource");

            migrationBuilder.DropTable(
                name: "ResourceAccessLevel");

            migrationBuilder.DropTable(
                name: "ResourceAction");

            migrationBuilder.DropTable(
                name: "LoanRecordStatus");

            migrationBuilder.DropTable(
                name: "Spend");

            migrationBuilder.DropTable(
                name: "CurrencyConverter");

            migrationBuilder.DropTable(
                name: "Account");

            migrationBuilder.DropTable(
                name: "AmountType");

            migrationBuilder.DropTable(
                name: "AccountGroup");

            migrationBuilder.DropTable(
                name: "AccountType");

            migrationBuilder.DropTable(
                name: "Currency");

            migrationBuilder.DropTable(
                name: "SpendType");

            migrationBuilder.DropTable(
                name: "FinancialEntity");

            migrationBuilder.DropTable(
                name: "PeriodDefinition");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "PeriodType");
        }
    }
}
