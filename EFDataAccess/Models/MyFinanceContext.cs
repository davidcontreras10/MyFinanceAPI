using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace EFDataAccess.Models
{
	public partial class MyFinanceContext : DbContext
	{
		public MyFinanceContext()
		{

		}

		public MyFinanceContext(DbContextOptions<MyFinanceContext> options)
			: base(options)
		{
		}

		#region DbSets

		public virtual DbSet<Account> Account { get; set; }
		public virtual DbSet<AccountGroup> AccountGroup { get; set; }
		public virtual DbSet<AccountInclude> AccountInclude { get; set; }
		public virtual DbSet<AccountPeriod> AccountPeriod { get; set; }
		public virtual DbSet<AccountType> AccountType { get; set; }
		public virtual DbSet<AmountType> AmountType { get; set; }
		public virtual DbSet<AppUser> AppUser { get; set; }
		public virtual DbSet<AppUserOwner> AppUserOwner { get; set; }
		public virtual DbSet<ApplicationModule> ApplicationModule { get; set; }
		public virtual DbSet<ApplicationResource> ApplicationResource { get; set; }
		public virtual DbSet<AutomaticTask> AutomaticTask { get; set; }
		public virtual DbSet<BccrWebServiceIndicator> BccrWebServiceIndicator { get; set; }
		public virtual DbSet<Currency> Currency { get; set; }
		public virtual DbSet<CurrencyConverter> CurrencyConverter { get; set; }
		public virtual DbSet<CurrencyConverterMethod> CurrencyConverterMethod { get; set; }
		public virtual DbSet<DailyJob> DailyJob { get; set; }
		public virtual DbSet<EntitiesSupported> EntitiesSupported { get; set; }
		public virtual DbSet<ExecutedTask> ExecutedTask { get; set; }
		public virtual DbSet<FinancialEntity> FinancialEntity { get; set; }
		public virtual DbSet<LoanRecord> LoanRecord { get; set; }
		public virtual DbSet<LoanRecordStatus> LoanRecordStatus { get; set; }
		public virtual DbSet<LoanSpend> LoanSpend { get; set; }
		public virtual DbSet<MethodsSupported> MethodsSupported { get; set; }
		public virtual DbSet<PeriodDefinition> PeriodDefinition { get; set; }
		public virtual DbSet<PeriodType> PeriodType { get; set; }
		public virtual DbSet<ResourceAccessLevel> ResourceAccessLevel { get; set; }
		public virtual DbSet<ResourceAction> ResourceAction { get; set; }
		public virtual DbSet<ResourceRequiredAccess> ResourceRequiredAccess { get; set; }
		public virtual DbSet<SpFinanceSpendByAccountsListTable> SpFinanceSpendByAccountsListTable { get; set; }
		public virtual DbSet<SpInTrxDef> SpInTrxDef { get; set; }
		public virtual DbSet<Spend> Spend { get; set; }
		public virtual DbSet<SpendDependencies> SpendDependencies { get; set; }
		public virtual DbSet<SpendOnPeriod> SpendOnPeriod { get; set; }
		public virtual DbSet<SpendType> SpendType { get; set; }
		public virtual DbSet<TransferRecord> TransferRecord { get; set; }
		public virtual DbSet<TransferTrxDef> TransferTrxDef { get; set; }
		public virtual DbSet<UserAssignedAccess> UserAssignedAccess { get; set; }
		public virtual DbSet<UserBankSummaryAccount> UserBankSummaryAccount { get; set; }
		public virtual DbSet<UserSpendType> UserSpendType { get; set; }

		#endregion

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<EFAccountIdName>().HasNoKey().ToView(null);
			modelBuilder.Entity<EFLoginResult>().HasNoKey().ToView(null);

			modelBuilder.Entity<Account>(entity =>
			{
				entity.OwnsOne(e => e.Notes);
				entity.Property(e => e.AccountTypeId).HasDefaultValueSql("((1))");

				entity.Property(e => e.HeaderColor).HasMaxLength(500);

				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);

				entity.HasOne(d => d.AccountGroup)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.AccountGroupId)
					.HasConstraintName("Account_FK_AccountGroupId");

				entity.HasOne(d => d.AccountType)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.AccountTypeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("Account_FK_AccountTypeId");

				entity.HasOne(d => d.Currency)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.CurrencyId)
					.HasConstraintName("Account_FK_CurrencyId");

				entity.HasOne(d => d.DefaultSpendType)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.DefaultSpendTypeId)
					.HasConstraintName("Account_FK_DefaultSpendTypeId");

				entity.HasOne(d => d.FinancialEntity)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.FinancialEntityId)
					.HasConstraintName("Account_FK_FinancialEntityId");

				entity.HasOne(d => d.PeriodDefinition)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.PeriodDefinitionId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("Account_FK_PeriodDefinitionId");

				entity.HasOne(d => d.User)
					.WithMany(p => p.Account)
					.HasForeignKey(d => d.UserId)
					.HasConstraintName("Account_FK_UserId");

				entity.HasIndex(p => p.UserId)
					.IsUnique(false);
			});

			modelBuilder.Entity<AccountGroup>(entity =>
			{
				entity.Property(e => e.AccountGroupName)
					.IsRequired()
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.Property(e => e.DisplayValue)
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.HasOne(d => d.User)
					.WithMany(p => p.AccountGroup)
					.HasForeignKey(d => d.UserId)
					.HasConstraintName("AccountGroup_FK_UserId");

				entity
					.Property(e => e.AccountGroupId)
					.ValueGeneratedOnAdd();
			});

			modelBuilder.Entity<AccountInclude>(entity =>
			{
				entity.HasKey(e => new { e.AccountId, e.AccountIncludeId });

				entity.HasOne(d => d.Account)
					.WithMany(p => p.AccountIncludeAccount)
					.HasForeignKey(d => d.AccountId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AccountInclude_FK_AccountId");

				entity.HasOne(d => d.AccountIncludeNavigation)
					.WithMany(p => p.AccountIncludeAccountIncludeNavigation)
					.HasForeignKey(d => d.AccountIncludeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AccountInclude_FK_AccountIncludeId");

				entity.HasOne(d => d.CurrencyConverterMethod)
					.WithMany(p => p.AccountInclude)
					.HasForeignKey(d => d.CurrencyConverterMethodId)
					.HasConstraintName("AccountInclude_FK_CurrencyConverterMethodId");
			});

			modelBuilder.Entity<AccountPeriod>(entity =>
			{
				entity.Property(e => e.EndDate).HasColumnType("datetime");

				entity.Property(e => e.InitialDate).HasColumnType("datetime");

				entity.HasOne(d => d.Account)
					.WithMany(p => p.AccountPeriod)
					.HasForeignKey(d => d.AccountId)
					.HasConstraintName("AccountPeriod_FK_AccountId");

				entity.HasOne(d => d.Currency)
					.WithMany(p => p.AccountPeriod)
					.HasForeignKey(d => d.CurrencyId)
					.HasConstraintName("AccountPeriod_FK_CurrencyId");
				entity.Property(p=>p.AccountPeriodId)
					.UseIdentityColumn();
			});

			modelBuilder.Entity<AccountType>(entity =>
			{
				entity.Property(e => e.AccountTypeName)
					.IsRequired()
					.HasMaxLength(500)
					.IsUnicode(false);
			});

			modelBuilder.Entity<AmountType>(entity =>
			{
				entity.HasIndex(e => e.AmountTypeName)
					.HasDatabaseName("AmountTypeName_unique")
					.IsUnique();

				entity.Property(e => e.AmountTypeName)
					.IsRequired()
					.HasMaxLength(100);
			});

			modelBuilder.Entity<AppUser>(entity =>
			{
				entity.HasKey(e => e.UserId)
					.HasName("PK_User");

				entity.HasIndex(e => e.Username)
					.HasDatabaseName("AppUser_Unq_Username")
					.IsUnique();

				entity.Property(e => e.UserId).HasDefaultValueSql("(newid())");

				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(e => e.Password)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(e => e.PrimaryEmail)
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.Property(e => e.Username)
					.IsRequired()
					.HasMaxLength(100);

				entity.HasOne(d => d.CreatedByUser)
					.WithMany(p => p.InverseCreatedByUser)
					.HasForeignKey(d => d.CreatedByUserId)
					.HasConstraintName("AppUser_FK_CreatedByUserId");
			});

			modelBuilder.Entity<AppUserOwner>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.UserId, e.OwnerUserId })
					.HasDatabaseName("ClusteredIndex-20210912-193500")
					.IsClustered();

				entity.HasOne(d => d.OwnerUser)
					.WithMany()
					.HasForeignKey(d => d.OwnerUserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AppUserOwner_FK_OwnerUserId");

				entity.HasOne(d => d.User)
					.WithMany()
					.HasForeignKey(d => d.UserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AppUserOwner_FK_UserId");
			});

			modelBuilder.Entity<ApplicationModule>(entity =>
			{
				entity.Property(e => e.ApplicationModuleId).ValueGeneratedNever();

				entity.Property(e => e.ApplicationModuleName)
					.HasMaxLength(100)
					.IsUnicode(false);
			});

			modelBuilder.Entity<ApplicationResource>(entity =>
			{
				entity.Property(e => e.ApplicationResourceId).ValueGeneratedNever();

				entity.Property(e => e.ApplicationResourceName)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<AutomaticTask>(entity =>
			{
				entity.HasIndex(e => e.AutomaticTaskId)
					.HasDatabaseName("AutomaticTask_Unq_AutomaticTaskId")
					.IsUnique();

				entity.Property(e => e.AutomaticTaskId).ValueGeneratedNever();

				entity.Property(e => e.Days).IsRequired();

				entity.Property(e => e.TaskDescription).HasMaxLength(400);

				entity.HasOne(d => d.Account)
					.WithMany(p => p.AutomaticTask)
					.HasForeignKey(d => d.AccountId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AutomaticTask_FK_AccountId");

				entity.HasOne(d => d.Currency)
					.WithMany(p => p.AutomaticTask)
					.HasForeignKey(d => d.CurrencyId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AutomaticTask_FK_CurrencyId");

				entity.HasOne(d => d.SpendType)
					.WithMany(p => p.AutomaticTask)
					.HasForeignKey(d => d.SpendTypeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AutomaticTask_FK_SpendTypeId");

				entity.HasOne(d => d.User)
					.WithMany(p => p.AutomaticTask)
					.HasForeignKey(d => d.UserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("AutomaticTask_FK_UserId");
			});

			modelBuilder.Entity<BccrWebServiceIndicator>(entity =>
			{
				entity.HasKey(e => e.EntityName);

				entity.Property(e => e.EntityName)
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.Property(e => e.PurchaseCode)
					.IsRequired()
					.HasMaxLength(50)
					.IsUnicode(false);

				entity.Property(e => e.SellCode)
					.IsRequired()
					.HasMaxLength(50)
					.IsUnicode(false);
			});

			modelBuilder.Entity<Currency>(entity =>
			{
				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(e => e.Symbol)
					.IsRequired()
					.HasMaxLength(10);
			});

			modelBuilder.Entity<CurrencyConverter>(entity =>
			{
				entity.HasOne(e => e.CurrencyOne)
					.WithMany()
					.HasForeignKey(e => e.CurrencyIdOne)
					.OnDelete(DeleteBehavior.NoAction);
				entity.HasOne(e => e.CurrencyTwo)
					.WithMany()
					.HasForeignKey(e => e.CurrencyIdTwo)
					.OnDelete(DeleteBehavior.NoAction);
			});


			modelBuilder.Entity<CurrencyConverterMethod>(entity =>
			{
				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);

				entity.HasOne(d => d.CurrencyConverter)
					.WithMany(p => p.CurrencyConverterMethod)
					.HasForeignKey(d => d.CurrencyConverterId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("CurrencyConverterMethod_FK_CurrencyIdOne");
			});

			modelBuilder.Entity<DailyJob>(entity =>
			{
				entity.Property(e => e.EventDesc)
					.HasMaxLength(100)
					.IsUnicode(false);

				entity.Property(e => e.JobDate).HasColumnType("datetime");
			});

			modelBuilder.Entity<EntitiesSupported>(entity =>
			{
				entity.Property(e => e.EntityName)
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.Property(e => e.EntitySearchKey)
					.IsRequired()
					.HasMaxLength(500)
					.IsUnicode(false);
			});

			modelBuilder.Entity<ExecutedTask>(entity =>
			{
				entity.HasIndex(e => e.ExecutedTaskId)
					.HasDatabaseName("ExecutedTask_Unq_ExecutedTaskId")
					.IsUnique();

				entity.Property(e => e.ExecuteDatetime).HasColumnType("datetime");

				entity.Property(e => e.ExecutionMsg)
					.HasMaxLength(500)
					.IsUnicode(false);

				entity.HasOne(d => d.AutomaticTask)
					.WithMany(p => p.ExecutedTask)
					.HasForeignKey(d => d.AutomaticTaskId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("ExecutedTask_FK_AutomaticTaskId");

				entity.HasOne(d => d.ExecutedByUser)
					.WithMany(p => p.ExecutedTask)
					.HasForeignKey(d => d.ExecutedByUserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("ExecutedTask_FK_UserId");
			});

			modelBuilder.Entity<FinancialEntity>(entity =>
			{
				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<LoanRecord>(entity =>
			{
				entity.HasIndex(e => e.SpendId)
					.HasDatabaseName("LoanRecord_UQ_SpendId")
					.IsUnique();

				entity.Property(e => e.LoanRecordName)
					.HasMaxLength(100)
					.IsUnicode(false);

				entity.HasOne(d => d.LoanRecordStatus)
					.WithMany(p => p.LoanRecord)
					.HasForeignKey(d => d.LoanRecordStatusId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("LoanRecord_FK_LoanRecordStatusId");

				entity.HasOne(d => d.Spend)
					.WithOne(p => p.LoanRecord)
					.HasForeignKey<LoanRecord>(d => d.SpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("LoanRecord_FK_SpendId");
			});

			modelBuilder.Entity<LoanRecordStatus>(entity =>
			{
				entity.Property(e => e.LoanRecordStatusId).ValueGeneratedNever();

				entity.Property(e => e.LoanRecordStatusName)
					.HasMaxLength(100)
					.IsUnicode(false);
			});

			modelBuilder.Entity<LoanSpend>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.LoanRecordId, e.SpendId })
					.HasDatabaseName("PK_LoanSpend")
					.IsUnique()
					.IsClustered();

				entity.HasOne(d => d.LoanRecord)
					.WithMany()
					.HasForeignKey(d => d.LoanRecordId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("LoanSpend_FK_LoanRecordId");

				entity.HasOne(d => d.Spend)
					.WithMany()
					.HasForeignKey(d => d.SpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("LoanSpend_FK_SpendId");
			});

			modelBuilder.Entity<MethodsSupported>(entity =>
			{
				entity.HasNoKey();

				entity.HasOne(d => d.EntitiesSupported)
					.WithMany()
					.HasForeignKey(d => d.EntitiesSupportedId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("MethodsSupported_FK_EntitiesSupportedId");
			});

			modelBuilder.Entity<PeriodDefinition>(entity =>
			{
				entity.Property(e => e.CuttingDate).HasMaxLength(500);

				entity.HasOne(d => d.PeriodType)
					.WithMany(p => p.PeriodDefinition)
					.HasForeignKey(d => d.PeriodTypeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("PeriodDefinition_FK_PeriodTypeId");
			});

			modelBuilder.Entity<PeriodType>(entity =>
			{
				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<ResourceAccessLevel>(entity =>
			{
				entity.Property(e => e.ResourceAccessLevelId).ValueGeneratedNever();

				entity.Property(e => e.ResourceAccessLevelName)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<ResourceAction>(entity =>
			{
				entity.Property(e => e.ResourceActionId).ValueGeneratedNever();

				entity.Property(e => e.ResourceActionName)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<ResourceRequiredAccess>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.ResourceActionId, e.ApplicationResourceId, e.ResourceAccessLevelId, e.ApplicationModuleId })
					.HasDatabaseName("ClusteredIndex-20210912-193313")
					.IsClustered();

				entity.HasOne(d => d.ApplicationModule)
					.WithMany()
					.HasForeignKey(d => d.ApplicationModuleId)
					.HasConstraintName("ResourceRequiredAccess_FK_ApplicationModuleId");

				entity.HasOne(d => d.ApplicationResource)
					.WithMany()
					.HasForeignKey(d => d.ApplicationResourceId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("ResourceRequiredAccess_FK_ApplicationResourceId");

				entity.HasOne(d => d.ResourceAccessLevel)
					.WithMany()
					.HasForeignKey(d => d.ResourceAccessLevelId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("ResourceRequiredAccess_FK_ResourceAccessLevelId");

				entity.HasOne(d => d.ResourceAction)
					.WithMany()
					.HasForeignKey(d => d.ResourceActionId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("ResourceRequiredAccess_FK_ResourceActionId");
			});

			modelBuilder.Entity<SpFinanceSpendByAccountsListTable>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.AccountId, e.AccountPeriodId, e.SpendId })
					.HasDatabaseName("ClusteredIndex-20210912-192807")
					.IsClustered();

				entity.Property(e => e.AccountCurrencySymbol)
					.HasMaxLength(1)
					.IsUnicode(false);

				entity.Property(e => e.AccountName)
					.HasMaxLength(100)
					.IsUnicode(false);

				entity.Property(e => e.EndDate).HasColumnType("datetime");

				entity.Property(e => e.InitialDate).HasColumnType("datetime");

				entity.Property(e => e.IsPending).HasDefaultValueSql("((0))");

				entity.Property(e => e.SetPaymentDate).HasColumnType("datetime");

				entity.Property(e => e.SpendCurrencyName)
					.HasMaxLength(100)
					.IsUnicode(false);

				entity.Property(e => e.SpendCurrencySymbol)
					.HasMaxLength(100)
					.IsUnicode(false);

				entity.Property(e => e.SpendDate).HasColumnType("datetime");

				entity.Property(e => e.SpendDescription).HasMaxLength(500);

				entity.Property(e => e.SpendTypeName)
					.HasMaxLength(100)
					.IsUnicode(false);
			});

			modelBuilder.Entity<SpInTrxDef>(entity =>
			{
				entity.HasIndex(e => e.SpInTrxDefId)
					.HasDatabaseName("SpInTrxDef_Unq_SpInTrxDefId")
					.IsUnique();

				entity.Property(e => e.SpInTrxDefId).ValueGeneratedNever();

				entity.HasOne(d => d.SpInTrxDefNavigation)
					.WithOne(p => p.SpInTrxDef)
					.HasForeignKey<SpInTrxDef>(d => d.SpInTrxDefId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("SpInTrxDef_FK_AutomaticTaskId");
			});

			modelBuilder.Entity<Spend>(entity =>
			{
				entity.Property(e => e.Denominator).HasDefaultValueSql("((1))");

				entity.Property(e => e.Description)
					.IsRequired()
					.HasMaxLength(500);

				entity.Property(e => e.Numerator).HasDefaultValueSql("((1))");

				entity.Property(e => e.SetPaymentDate).HasColumnType("datetime");

				entity.Property(e => e.SpendDate).HasColumnType("datetime");

				entity.HasOne(d => d.AmountCurrency)
					.WithMany(p => p.Spend)
					.HasForeignKey(d => d.AmountCurrencyId)
					.HasConstraintName("Spend_FK_AmountCurrencyId");

				entity.HasOne(d => d.AmountType)
					.WithMany(p => p.Spend)
					.HasForeignKey(d => d.AmountTypeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("Spend_FK_AmountTypeId");

				entity.HasOne(d => d.SpendType)
					.WithMany(p => p.Spend)
					.HasForeignKey(d => d.SpendTypeId)
					.HasConstraintName("Spend_FK_SpendTypeId");
			});

			modelBuilder.Entity<SpendDependencies>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.SpendId, e.DependencySpendId })
					.HasDatabaseName("PK_SpendDependencies")
					.IsUnique()
					.IsClustered();

				entity.HasOne(d => d.Spend)
					.WithMany()
					.HasForeignKey(d => d.SpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("SpendDependencies_FK_SpendId");

				entity.HasOne(d => d.DependencySpend)
					.WithMany()
					.HasForeignKey(d => d.DependencySpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("SpendDependencies_Dep_FK_SpendId");
			});

			modelBuilder.Entity<SpendOnPeriod>(entity =>
			{
				entity.HasKey(e => new { e.SpendId, e.AccountPeriodId });

				entity.HasOne(d => d.AccountPeriod)
					.WithMany(p => p.SpendOnPeriod)
					.HasForeignKey(d => d.AccountPeriodId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("SpendOnPeriod_FK_AccountPeriodId");

				entity.HasOne(d => d.CurrencyConverterMethod)
					.WithMany(p => p.SpendOnPeriod)
					.HasForeignKey(d => d.CurrencyConverterMethodId)
					.HasConstraintName("SpendOnPeriod_FK_CurrencyConverterMethodId");

				entity.HasOne(d => d.Spend)
					.WithMany(p => p.SpendOnPeriod)
					.HasForeignKey(d => d.SpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("SpendOnPeriod_FK_SpendId");
			});

			modelBuilder.Entity<SpendType>(entity =>
			{
				entity.Property(e => e.Description).HasMaxLength(500);

				entity.Property(e => e.Name)
					.IsRequired()
					.HasMaxLength(500);
			});

			modelBuilder.Entity<TransferRecord>(entity =>
			{
				entity.HasKey(e => new { e.TransferRecordId, e.SpendId });

				entity.HasIndex(e => new { e.TransferRecordId, e.SpendId })
					.HasDatabaseName("ClusteredIndex-20210912-192222")
					.IsClustered();

				entity.HasOne(d => d.Spend)
					.WithMany()
					.HasForeignKey(d => d.SpendId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("TransferRecord_FK_SpendId");
			});

			modelBuilder.Entity<TransferTrxDef>(entity =>
			{
				entity.HasIndex(e => e.TransferTrxDefId)
					.HasDatabaseName("TransferTrxDef_Unq_TransferTrxDefId")
					.IsUnique();

				entity.Property(e => e.TransferTrxDefId).ValueGeneratedNever();

				entity.HasOne(d => d.ToAccount)
					.WithMany(p => p.TransferTrxDef)
					.HasForeignKey(d => d.ToAccountId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("TransferTrxDef_FK_ToAccountId");

				entity.HasOne(d => d.TransferTrxDefNavigation)
					.WithOne(p => p.TransferTrxDef)
					.HasForeignKey<TransferTrxDef>(d => d.TransferTrxDefId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("TransferTrxDef_FK_AutomaticTaskId");
			});

			modelBuilder.Entity<UserAssignedAccess>(entity =>
			{
				entity.HasNoKey();

				entity.HasIndex(e => new { e.UserId, e.ResourceActionId, e.ApplicationResourceId, e.ResourceAccessLevelId })
					.HasDatabaseName("ClusteredIndex-20210912-193345")
					.IsClustered();

				entity.HasOne(d => d.ApplicationResource)
					.WithMany()
					.HasForeignKey(d => d.ApplicationResourceId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserAssignedAccess_FK_ApplicationResourceId");

				entity.HasOne(d => d.ResourceAccessLevel)
					.WithMany()
					.HasForeignKey(d => d.ResourceAccessLevelId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserAssignedAccess_FK_ResourceAccessLevelId");

				entity.HasOne(d => d.ResourceAction)
					.WithMany()
					.HasForeignKey(d => d.ResourceActionId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserAssignedAccess_FK_ResourceActionId");

				entity.HasOne(d => d.User)
					.WithMany()
					.HasForeignKey(d => d.UserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserAssignedAccess_FK_UserId");
			});

			modelBuilder.Entity<UserBankSummaryAccount>(entity =>
			{
				entity.HasKey(e => new { e.AccountId, e.UserId });

				entity.HasOne(d => d.Account)
					.WithMany(p => p.UserBankSummaryAccount)
					.HasForeignKey(d => d.AccountId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserBankSummaryAccount_FK_AccountId");

				entity.HasOne(d => d.User)
					.WithMany(p => p.UserBankSummaryAccount)
					.HasForeignKey(d => d.UserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserBankSummaryAccount_FK_UserId");
			});

			modelBuilder.Entity<UserSpendType>(entity =>
			{
				entity.HasKey(e => new { e.UserId, e.SpendTypeId });

				entity.HasOne(d => d.SpendType)
					.WithMany(p => p.UserSpendType)
					.HasForeignKey(d => d.SpendTypeId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserSpendType_FK_SpendTypeId");

				entity.HasOne(d => d.User)
					.WithMany(p => p.UserSpendType)
					.HasForeignKey(d => d.UserId)
					.OnDelete(DeleteBehavior.ClientSetNull)
					.HasConstraintName("UserSpendType_FK_UserId");
			});

			OnModelCreatingPartial(modelBuilder);
		}

		partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
	}
}
