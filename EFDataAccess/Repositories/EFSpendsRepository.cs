﻿using EFDataAccess.Extensions;
using EFDataAccess.Helpers;
using EFDataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceBackend.Services;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.Utilities;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Spend = EFDataAccess.Models.Spend;

namespace EFDataAccess.Repositories
{
	public class EFSpendsRepository : BaseEFRepository, ISpendsRepository
	{
		private readonly ITrxExchangeService _trxExchangeService;
		private readonly ILogger<EFSpendsRepository> _logger;

		public EFSpendsRepository(MyFinanceContext context, ITrxExchangeService trxExchangeService, ILogger<EFSpendsRepository> logger) : base(context)
		{
			context.Database.SetCommandTimeout(180);
			_trxExchangeService = trxExchangeService;
			_logger = logger;
		}

		#region Publics

		public async Task<IReadOnlyCollection<int>> AppTransactionsWithBankTrxAsync(IEnumerable<int> trxIds)
		{
			if (trxIds == null || !trxIds.Any())
			{
				return [];
			}

			var transactionIds = await Context.Spend.AsNoTracking()
			.Where(trx => trxIds.Contains(trx.SpendId) && trx.SpendOnPeriod.Any(x => x.BankTrxFinancialEntityId > 0))
			.Select(trx => trx.SpendId)
			.ToListAsync();
			return transactionIds;
		}

		public async Task<SpendOnPeriodId> GetSpendOnPeriodAccountBySpendIdAccountIdAsync(int spendId, int accountId)
		{
			var spendOnPeriod = await Context.SpendOnPeriod.AsNoTracking()
				.Where(sop => sop.SpendId == spendId && sop.AccountPeriod.AccountId == accountId)
				.Select(sop => new SpendOnPeriodId(sop.SpendId, sop.AccountPeriodId))
				.FirstOrDefaultAsync();

			return spendOnPeriod;
		}

		public async Task<IEnumerable<TrxItemModifiedRecord>> AddMultipleTransactionsAsync(IReadOnlyCollection<ClientConvertedTrxModel> transactions)
		{
			if (transactions == null || transactions.Count == 0) return [];

			var modifiedAccs = new List<IdentifiableRequest<SpendOnPeriod>>();
			foreach (var transaction in transactions)
			{
				modifiedAccs.AddRange(await AddSpendAsync(transaction));
			}

			if (modifiedAccs.Count == 0) return [];
			await Context.SaveChangesAsync();
			var modifiedRecords = modifiedAccs.Select(sop =>
			new TrxItemModifiedRecord(sop.Request.AccountPeriod.AccountId ?? 0, true, sop.Request.SpendId, sop.Request.AccountPeriodId, sop.RequestId));
			return modifiedRecords;
		}

		private async Task<IEnumerable<IdentifiableRequest<SpendOnPeriod>>> AddSpendAsync(ClientConvertedTrxModel clientConvertedTrxModel)
		{
			ArgumentNullException.ThrowIfNull(clientConvertedTrxModel);
			if (clientConvertedTrxModel.PeriodTransactions == null || !clientConvertedTrxModel.PeriodTransactions.Any())
			{
				throw new ArgumentException(@"PeriodTransactions is null", nameof(clientConvertedTrxModel));
			}
			var setPaymentDate = clientConvertedTrxModel.IsPending ? null : (DateTime?)clientConvertedTrxModel.TrxDate;
			var clientAccountIncluded = clientConvertedTrxModel.PeriodTransactions;
			var accountIds = clientAccountIncluded.Select(x => x.AccountId);
			var spendDate = clientConvertedTrxModel.TrxDate;
			var spend = new Spend
			{
				AmountCurrencyId = clientConvertedTrxModel.CurrencyId,
				AmountTypeId = (int)clientConvertedTrxModel.AmountTypeId,
				Denominator = clientConvertedTrxModel.AmountDenominator,
				Description = clientConvertedTrxModel.Description,
				IsPending = clientConvertedTrxModel.IsPending,
				Numerator = clientConvertedTrxModel.AmountNumerator,
				OriginalAmount = clientConvertedTrxModel.OriginalAmount,
				SpendDate = spendDate,
				SetPaymentDate = setPaymentDate,
				SpendTypeId = clientConvertedTrxModel.TrxTypeId,
				UtcRecordDate = DateTime.UtcNow,
				IsPurchase = clientConvertedTrxModel.IsPurchase,
				CurrencyConverterMethodId = clientConvertedTrxModel.MethodId
			};

			await Context.Spend.AddAsync(spend);
			var accountPeriods = await Context.AccountPeriod
				.Where(accp =>
					accountIds.Contains(accp.AccountId ?? 0)
					&& spendDate >= accp.InitialDate && spendDate < accp.EndDate)
				.ToListAsync();
			var modifiedAccs = new List<IdentifiableRequest<SpendOnPeriod>>();
			foreach (var accountPeriod in accountPeriods)
			{
				var accountIncluded = clientAccountIncluded.First(acc => acc.AccountId == accountPeriod.AccountId);
				var spendOnPeriod = new SpendOnPeriod
				{
					AccountPeriod = accountPeriod,
					Spend = spend,
					CurrencyConverterMethodId = accountIncluded.CurrencyConverterMethodId,
					Denominator = accountIncluded.Denominator,
					Numerator = accountIncluded.Numerator,
					IsOriginal = accountIncluded.IsOriginal
				};

				modifiedAccs.Add(new IdentifiableRequest<SpendOnPeriod>(spendOnPeriod, clientConvertedTrxModel.RequestId ?? Guid.NewGuid()));
				accountPeriod.SpendOnPeriod.Add(spendOnPeriod);
			}

			return modifiedAccs;
		}


		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel)
		{
			ArgumentNullException.ThrowIfNull(clientAddSpendModel);
			if (clientAddSpendModel.OriginalAccountData == null)
				throw new ArgumentException(@"OriginalAccountData is null", nameof(clientAddSpendModel));
			await ValidateSpendCurrencyConvertibleValuesAsync(clientAddSpendModel);
			SpendsDataHelper.ValidateAmountType(clientAddSpendModel, false);

			var setPaymentDate = clientAddSpendModel.IsPending ? null : (DateTime?)clientAddSpendModel.PaymentDate;
			var clientAccountIncluded = await GetConvertedAccountIncludedAsync(clientAddSpendModel);
			var accountIds = clientAccountIncluded.Select(x => x.AccountId);
			var spendDate = clientAddSpendModel.SpendDate;
			var spend = new Spend
			{
				AmountCurrencyId = clientAddSpendModel.CurrencyId,
				AmountTypeId = (int)clientAddSpendModel.AmountTypeId,
				Denominator = clientAddSpendModel.AmountDenominator,
				Description = clientAddSpendModel.Description,
				IsPending = clientAddSpendModel.IsPending,
				Numerator = clientAddSpendModel.AmountNumerator,
				OriginalAmount = clientAddSpendModel.Amount,
				SpendDate = spendDate,
				SetPaymentDate = setPaymentDate,
				SpendTypeId = clientAddSpendModel.SpendTypeId
			};

			await Context.Spend.AddAsync(spend);
			var accountPeriods = await Context.AccountPeriod
				.Where(accp =>
					accountIds.Contains(accp.AccountId ?? 0)
					&& spendDate >= accp.InitialDate && spendDate < accp.EndDate)
				.ToListAsync();
			var modifiedAccs = new List<SpendItemModified>();
			foreach (var accountPeriod in accountPeriods)
			{
				var accountIncluded = clientAccountIncluded.First(acc => acc.AccountId == accountPeriod.AccountId);
				var spendOnPeriod = new SpendOnPeriod
				{
					AccountPeriod = accountPeriod,
					Spend = spend,
					CurrencyConverterMethodId = accountIncluded.CurrencyConverterMethodId,
					Denominator = accountIncluded.Denominator,
					Numerator = accountIncluded.Numerator,
					IsOriginal = accountIncluded.IsOriginal
				};

				modifiedAccs.Add(new SpendItemModified
				{
					AccountId = accountPeriod.AccountId ?? 0,
					IsModified = true
				});
				accountPeriod.SpendOnPeriod.Add(spendOnPeriod);
			}

			await Context.SaveChangesAsync();
			modifiedAccs.ForEach(item =>
			{
				item.SpendId = spend.SpendId;
			});

			return modifiedAccs;
		}

		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId)
		{
			var clientAddSpendModel = await CreateClientAddSpendModelAsync(clientBasicAddSpend, accountPeriodId);
			return await AddSpendAsync(clientAddSpendModel);
		}

		public async Task AddSpendDependencyAsync(int spendId, int dependencySpendId)
		{
			await AddSpendDependencyAsync(spendId, dependencySpendId);
		}

		public async Task<ClientAddSpendModel> CreateClientAddSpendModelAsync(ClientBasicAddSpend clientBasicAddSpend, int accountPeriodId)
		{
			var accountIds = await Context.AccountPeriod.AsNoTracking()
				.Where(accp => accp.AccountPeriodId == accountPeriodId)
				.Select(accp => accp.AccountId)
				.ToListAsync();
			var accountId = accountIds.First() ?? 0;
			if (accountId == 0)
			{
				throw new Exception($"No account for period {accountPeriodId}");
			}

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
				RequestId = clientBasicAddSpend.RequestId
			};

			return clientAddSpendModel;
		}

		public async Task<IEnumerable<SpendItemModified>> DeleteTransactionsAsync(IReadOnlyCollection<int> transactionIds)
		{
			try
			{
				await ValidateSpendIdInLoanAsync(transactionIds);
				var spendsToDelete = new List<Spend>
				{
					await Context.Spend.Where(sp => transactionIds.Contains(sp.SpendId)).FirstAsync()
				};

				var spendDependencies = await GetSpendDependenciesAsync(transactionIds);
				var spendIds = new List<int>();
				spendIds.AddRange(transactionIds);
				spendIds.AddRange(spendDependencies.Select(sp => sp.SpendId));
				var affectedAccounts = await Context.SpendOnPeriod.AsNoTracking()
					.Where(sop => spendIds.Contains(sop.SpendId))
					.Include(sop => sop.AccountPeriod)
					.Select(sop => new SpendItemModified
					{
						SpendId = sop.SpendId,
						AccountId = sop.AccountPeriod.AccountId ?? 0,
						IsModified = true
					})
					.ToArrayAsync();
				Context.LoanSpend.RemoveWhere(x => spendIds.Contains(x.SpendId));
				Context.SpendDependencies.RemoveWhere(x => spendIds.Contains(x.SpendId));
				Context.TransferRecord.RemoveWhere(x => spendIds.Contains(x.SpendId));
				Context.SpendOnPeriod.RemoveWhere(x => spendIds.Contains(x.SpendId));
				Context.Spend.RemoveWhere(x => spendIds.Contains(x.SpendId));
				Context.AppTransfers.RemoveWhere(x => spendIds.Contains(x.SourceAppTrxId) || spendIds.Contains(x.DestinationAppTrxId));
				return affectedAccounts;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				throw;
			}

		}

		public async Task<IEnumerable<SpendItemModified>> EditSpendAsync(ClientEditSpendModel model)
		{
			if (model == null || model.SpendId == 0 || string.IsNullOrEmpty(model.UserId) || !model.ModifyList.Any() ||
				model.ModifyList.Any(i => i == 0) || model.ModifyList.Any(i => !((int)i).TryParseEnum<ClientEditSpendModel.Field>(out _)))
				throw new Exception("Invalid parameters");
			SpendsDataHelper.ValidateAmountType(model, !model.ModifyList.Any(i => i == ClientEditSpendModel.Field.AmountType));
			var spendIds = new List<int>
			{
				model.SpendId
			};

			var hasBankTrx = await Context.SpendOnPeriod.AsNoTracking()
				.Include(sp => sp.BankTransaction)
				.Where(sp => sp.SpendId == model.SpendId)
				.AnyAsync(x => x.BankTransaction != null);
			var withTransferSpends = await AppTrxsHasTransfersAsync(spendIds);
			var hasAmountTypeChange = model.ModifyList.Any(i => i == ClientEditSpendModel.Field.AmountType);
			if (withTransferSpends && hasAmountTypeChange)
			{
				throw new ServiceException("Cannot modify amount type for transactions with transfers", HttpStatusCode.BadRequest);
			}
			if (hasBankTrx && hasAmountTypeChange)
			{
				throw new ServiceException("Cannot modify amount type for bank transactions", HttpStatusCode.BadRequest);
			}

			var transferRecordId = await Context.TransferRecord
				.Where(tr => tr.SpendId == model.SpendId)
				.Select(tr => tr.TransferRecordId)
				.FirstOrDefaultAsync();
			if (transferRecordId > 0)
			{
				var transferSpendIds = await Context.TransferRecord
					.Where(tr => tr.TransferRecordId == transferRecordId)
					.Select(tr => tr.SpendId)
					.ToListAsync();
				spendIds.AddRange(transferSpendIds);
			}

			var modifySpends = await Context.Spend
				.Where(sp => spendIds.Contains(sp.SpendId))
				.Include(sp => sp.SpendOnPeriod)
					.ThenInclude(sop => sop.AccountPeriod)
				.ToListAsync();
			var modifyList = new List<SpendItemModified>();
			foreach (var spend in modifySpends)
			{
				if (model.ModifyList.Any(m => m == ClientEditSpendModel.Field.Description))
				{
					spend.Description = model.Description;
				}

				if (model.ModifyList.Any(m => m == ClientEditSpendModel.Field.SpendType))
				{
					spend.SpendTypeId = model.SpendTypeId;
				}

				if (model.ModifyList.Any(m => m == ClientEditSpendModel.Field.AmountType))
				{
					spend.AmountTypeId = (int)model.AmountTypeId;
				}

				modifyList.AddRange(spend.SpendOnPeriod.Select(sop => new SpendItemModified
				{
					AccountId = sop.AccountPeriod.AccountId ?? 0,
					IsModified = true,
					SpendId = sop.SpendId
				}));
			}

			await Context.SaveChangesAsync();
			return modifyList;
		}

		public async Task<IEnumerable<SpendItemModified>> EditSpendAsync(FinanceSpend financeSpend)
		{
			await ValidateSpendCurrencyConvertibleValuesAsync(financeSpend);
			if (financeSpend.OriginalAccountData == null)
				throw new ArgumentException(@"OriginalAccountData is null", nameof(financeSpend));
			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(financeSpend);
			var accountPeriods = Context.AccountPeriod.AsNoTracking()
				.Where(accp => accountIds.Contains(accp.Account.AccountId)
					&& financeSpend.SpendDate >= accp.InitialDate
					&& financeSpend.SpendDate < accp.EndDate)
				.Include(accp => accp.Account);
			var accountCurrencyPairList = accountPeriods.Select(accp => new AccountCurrencyPair
			{
				AccountId = accp.Account.AccountId,
				CurrencyId = accp.Account.CurrencyId ?? 0
			});
			var accountIncludes = await _trxExchangeService.ConvertTrxCurrencyAsync(financeSpend, accountCurrencyPairList.ToList());
			var spend = await Context.Spend.Where(sp => sp.SpendId == financeSpend.SpendId).FirstOrDefaultAsync();
			spend.SetPaymentDate = financeSpend.SetPaymentDate;
			spend.SpendDate = financeSpend.SpendDate;
			spend.IsPending = financeSpend.IsPending;
			spend.Numerator = financeSpend.AmountNumerator;
			spend.Denominator = financeSpend.AmountDenominator;
			var toRemoveSpendOnPeriods = await Context.SpendOnPeriod
				.Where(sop => sop.SpendId == financeSpend.SpendId)
				.Include(sop => sop.AccountPeriod)
				.Include(sop => sop.BankTransaction)
				.ToListAsync();
			Context.SpendOnPeriod.RemoveRange(toRemoveSpendOnPeriods);
			var spendOnPeriods = new List<SpendOnPeriod>();
			var affected = new List<SpendItemModified>();
			foreach (var accountInclude in accountIncludes)
			{
				var removedSop = toRemoveSpendOnPeriods.FirstOrDefault(sop => sop.AccountPeriod.AccountId == accountInclude.AccountId);
				var accountPeriod = accountPeriods.Single(accp => accp.Account.AccountId == accountInclude.AccountId);
				spendOnPeriods.Add(new SpendOnPeriod
				{
					AccountPeriodId = accountPeriod.AccountPeriodId,
					CurrencyConverterMethodId = accountInclude.CurrencyConverterMethodId,
					Denominator = accountInclude.Denominator,
					Numerator = accountInclude.Numerator,
					IsOriginal = accountInclude.IsOriginal,
					SpendId = spend.SpendId,
					PendingUpdate = accountInclude.PendingUpdate,
					BankTransaction = removedSop?.BankTransaction
				});

				affected.Add(new SpendItemModified
				{
					AccountId = accountInclude.AccountId,
					IsModified = true,
					SpendId = spend.SpendId
				});
			}

			await Context.SpendOnPeriod.AddRangeAsync(spendOnPeriods);
			await Context.SaveChangesAsync();
			return affected;
		}

		public async Task<AccountFinanceViewModel> GetAccountFinanceViewModelAsync(int accountPeriodId, string userId)
		{
			var requestItems = new List<ClientAccountFinanceViewModel>
			{
				new() {
					AccountPeriodId = accountPeriodId,
					LoanSpends = false,
					PendingSpends = true
				}
			};

			var viewModels = await GetAccountFinanceViewModelAsync(requestItems, null);
			return viewModels.First();
		}

		public async Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModelAsync(IEnumerable<ClientAccountFinanceViewModel> requestItems, string userId, DateTime? dateTime)
		{
			var res = await GetAccountFinanceViewModelAsync(requestItems.ToList(), dateTime);
			return res;
		}

		public async Task<IEnumerable<ClientAddSpendAccount>> GetAccountMethodConversionInfoAsync(int? accountId, int? accountPeriodId, string userId, int currencyId)
		{
			if (string.IsNullOrWhiteSpace(userId))
			{
				throw new ArgumentNullException(nameof(userId));
			}

			if (currencyId == 0)
			{
				throw new ArgumentNullException(nameof(currencyId));
			}

			ValidateEitherOrAccountIdValues(accountId, accountPeriodId);
			if (accountId == null || accountId == 0)
			{
				accountId = await Context.AccountPeriod.AsNoTracking()
					.Where(accp => accp.AccountPeriodId == accountPeriodId)
					.Select(accp => accp.AccountId)
					.FirstAsync();
			}

			var account = await Context.Account.AsNoTracking()
				.Where(acc => acc.AccountId == accountId)
				.Include(acc => acc.AccountIncludeAccount)
					.ThenInclude(acci => acci.AccountIncludeNavigation)
				.Include(acc => acc.AccountIncludeAccount)
					.ThenInclude(acci => acci.CurrencyConverterMethod)
						.ThenInclude(acci => acci.CurrencyConverter)
				.FirstAsync();
			var accountIncludeData = account.AccountIncludeAccount;
			var applicableCcms = await Context.CurrencyConverterMethod.AsNoTracking()
				.Include(ccm => ccm.CurrencyConverter)
				.Where(ccm => ccm.CurrencyConverter.CurrencyIdOne == currencyId)
				.ToListAsync();
			var accountIncludeItems = accountIncludeData.Select(acci => new
			{
				DestinationAccount = acci.AccountIncludeNavigation,
				CurrencyConverterMethod = acci.CurrencyConverterMethod
			}).ToList();
			accountIncludeItems.Add(new
			{
				DestinationAccount = account,
				CurrencyConverterMethod = (CurrencyConverterMethod)null
			});

			var clientAddSpendAccounts = new List<ClientAddSpendAccount>();
			foreach (var accItem in accountIncludeItems)
			{
				if (accItem.DestinationAccount.CurrencyId == currencyId)
				{
					var defaultccm = applicableCcms.FirstOrDefault(ccm =>
					ccm.CurrencyConverter.CurrencyIdOne == currencyId
					&& ccm.IsDefault != null && ccm.IsDefault.Value);
					if (defaultccm == null)
					{
						throw new Exception($"Default ccm for {currencyId} does not exist");
					}

					clientAddSpendAccounts.Add(new ClientAddSpendAccount
					{
						AccountId = accItem.DestinationAccount.AccountId,
						ConvertionMethodId = defaultccm.CurrencyConverterId
					});

					continue;
				}

				var currencyConverter = accItem.CurrencyConverterMethod?.CurrencyConverter;
				if (currencyConverter != null
					&& currencyConverter.CurrencyIdOne == currencyId
					&& currencyConverter.CurrencyIdTwo == accItem.DestinationAccount.CurrencyId)
				{
					clientAddSpendAccounts.Add(new ClientAddSpendAccount
					{
						AccountId = accItem.DestinationAccount.AccountId,
						ConvertionMethodId = accItem.CurrencyConverterMethod.CurrencyConverterMethodId
					});

					continue;
				}

				var itemApplicableCcms = applicableCcms.Where(ccm =>
					ccm.CurrencyConverter.CurrencyIdTwo == accItem.DestinationAccount.CurrencyId);
				if (!itemApplicableCcms.Any())
				{
					throw new Exception($"Unable to convert from ${currencyId} to {accItem.DestinationAccount.CurrencyId}");
				}

				var selectedCcm = Validation.IsNotNullOrDefault(accItem.DestinationAccount.FinancialEntityId)
					? applicableCcms.FirstOrDefault(ccm =>
						ccm.FinancialEntityId == accItem.DestinationAccount.FinancialEntityId
						&& ccm.CurrencyConverter.CurrencyIdTwo == accItem.DestinationAccount.CurrencyId)
					: null;
				if (selectedCcm != null)
				{
					clientAddSpendAccounts.Add(new ClientAddSpendAccount
					{
						AccountId = accItem.DestinationAccount.AccountId,
						ConvertionMethodId = selectedCcm.CurrencyConverterMethodId
					});

					continue;
				}

				selectedCcm = applicableCcms.FirstOrDefault(ccm =>
						ccm.CurrencyConverter.CurrencyIdTwo == accItem.DestinationAccount.CurrencyId);
				if (selectedCcm == null)
				{
					throw new Exception($"Unable to convert from ${currencyId} to {accItem.DestinationAccount.CurrencyId}");
				}

				clientAddSpendAccounts.Add(new ClientAddSpendAccount
				{
					AccountId = accItem.DestinationAccount.AccountId,
					ConvertionMethodId = selectedCcm.CurrencyConverterMethodId
				});
			}

			return clientAddSpendAccounts;
		}

		public async Task<IEnumerable<AccountCurrencyPair>> GetAccountsCurrencyAsync(IEnumerable<int> accountIdsArray)
		{
			return await Context.Account.AsNoTracking()
				.Where(acc => accountIdsArray.Contains(acc.AccountId))
				.Select(acc => new AccountCurrencyPair
				{
					AccountId = acc.AccountId,
					CurrencyId = acc.CurrencyId ?? 0
				}).ToListAsync();
		}

		public async Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModelAsync(IEnumerable<int> accountPeriodIds, string userId)
		{
			var accountPeriods = await Context.AccountPeriod.AsNoTracking()
				.Where(accp => accountPeriodIds.Contains(accp.AccountPeriodId))
				.Include(accp => accp.Account)
					.ThenInclude(acc => acc.AccountIncludeAccount)
				.ToListAsync();
			var userGuid = new Guid(userId);
			var spendTypes = await Context.UserSpendType.AsNoTracking()
				.Where(uspt => uspt.UserId == userGuid)
				.Include(uspt => uspt.SpendType)
				.Select(uspt => uspt.SpendType)
				.ToListAsync();
			var currencyConverterMethods = await Context.CurrencyConverterMethod.AsNoTracking()
				.Include(ccm => ccm.CurrencyConverter)
					.ThenInclude(cc => cc.CurrencyOne)
				.ToListAsync();
			var accountViewModels = new List<AddSpendViewModel>();
			foreach (var accountPeriod in accountPeriods)
			{
				var endDate = accountPeriod.EndDate ?? new DateTime();
				var initialDate = accountPeriod.InitialDate ?? new DateTime();
				var suggesteDate = DateTime.Now;
				if (suggesteDate > endDate)
				{
					suggesteDate = endDate.AddMinutes(-1);
				}

				if (suggesteDate < initialDate)
				{
					suggesteDate = initialDate;
				}
				var addSpendViewModel = new AddSpendViewModel
				{
					AccountId = accountPeriod.Account.AccountId,
					AccountName = accountPeriod.Account.Name,
					AccountPeriodId = accountPeriod.AccountPeriodId,
					CurrencyId = accountPeriod.Account.CurrencyId ?? 0,
					EndDate = endDate,
					GlobalOrder = accountPeriod.Account.Position ?? 0,
					InitialDate = initialDate,
					SpendTypeViewModels = spendTypes.Select(spt => spt.ToSpendTypeViewModel(accountPeriod.Account.DefaultSpendTypeId)),
					SuggestedDate = suggesteDate,
					IsDefaultPending = accountPeriod.Account.DefaultSelectIsPending
				};
				var currencyMethods = currencyConverterMethods
					.Where(ccm => ccm.CurrencyConverter.CurrencyIdTwo == accountPeriod.Account.CurrencyId);
				addSpendViewModel.SupportedCurrencies = CreateCurrencyViewModelFromMethods(currencyConverterMethods, accountPeriod.Account);

				accountViewModels.Add(addSpendViewModel);
			}

			return accountViewModels;
		}

		public async Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModelAsync(int accountPeriodId, int spendId, string userId)
		{
			var userGuid = new Guid(userId);
			var spendTypes = await Context.UserSpendType.AsNoTracking()
				.Where(uspt => uspt.UserId == userGuid)
				.Include(uspt => uspt.SpendType)
				.Select(uspt => uspt.SpendType)
				.ToListAsync();
			var spend = await Context.Spend.AsNoTracking()
				.Where(sp => sp.SpendId == spendId)
				.Include(sp => sp.AmountCurrency)
				.Include(sp => sp.SpendOnPeriod)
					.ThenInclude(sop => sop.AccountPeriod)
						.ThenInclude(accp => accp.Account)
							.ThenInclude(acc => acc.Currency)
				.Include(sp => sp.SpendOnPeriod)
					.ThenInclude(sop => sop.CurrencyConverterMethod)
				.Include(sp => sp.SourceAppTransfer)
					.ThenInclude(tr => tr.DestinationAppTrx)
						.ThenInclude(sp => sp.SpendOnPeriod)
							.ThenInclude(sop => sop.AccountPeriod)
								.ThenInclude(accp => accp.Account)
				.Include(sp => sp.DestinationAppTransfer)
					.ThenInclude(tr => tr.SourceAppTrx)
						.ThenInclude(sp => sp.SpendOnPeriod)
							.ThenInclude(sop => sop.AccountPeriod)
								.ThenInclude(accp => accp.Account)
				.FirstAsync();
			var otherSops = spend.SpendOnPeriod.Where(sop => !sop.IsOriginal.Value);
			var originalSop = spend.SpendOnPeriod.First(sop => sop.IsOriginal.Value);
			var original = spend.SpendOnPeriod.First(sop => sop.IsOriginal != null && sop.IsOriginal.Value);
			var spendViewModel = original.ToSpendSpendViewModel<FinanceSpendViewModel>();
			var accountPeriods = spend.SpendOnPeriod.Select(sp => sp.AccountPeriod);
			var dateRange = GetDateRange(accountPeriods, spend.SpendDate);
			var method = new MethodId
			{
				Id = originalSop.CurrencyConverterMethod.CurrencyConverterMethodId,
				IsDefault = originalSop.CurrencyConverterMethod.IsDefault ?? false,
				IsSelected = true,
				Name = originalSop.CurrencyConverterMethod.Name
			};
			var currency = new CurrencyViewModel
			{
				AccountId = originalSop.AccountPeriod.AccountId ?? 0,
				CurrencyId = spend.AmountCurrencyId ?? 0,
				CurrencyName = spend.AmountCurrency.Name,
				Symbol = spend.AmountCurrency.Symbol,
				MethodIds = [method],
				IsDefault = true
			};

			var accountIncludeViewModels = otherSops.Select(sop => new AccountIncludeViewModel
			{
				AccountId = sop.AccountPeriod.AccountId ?? 0,
				AccountName = sop.AccountPeriod.Account.Name,
				Amount = new SpendAmount
				{
					Value = (float)sop.GetAmount(),
					CurrencyId = sop.AccountPeriod.Account.Currency.CurrencyId,
					CurrencyName = sop.AccountPeriod.Account.Currency.Name,
					CurrencySymbol = sop.AccountPeriod.Account.Currency.Symbol
				},
				IsDefault = true,
				IsSelected = true,
				MethodIds = [sop.CurrencyConverterMethod.ToMethodId(true, true)]
			});

			return
			[
				new EditSpendViewModel
				{
					AccountId = originalSop.AccountPeriod.AccountId ?? 0,
					CurrencyId = spend.AmountCurrencyId ?? 0,
					AccountPeriodId = originalSop.AccountPeriodId,
					AccountName = originalSop.AccountPeriod.Account.Name,
					EndDate = originalSop.AccountPeriod?.EndDate ?? DateTime.MinValue,
					InitialDate = originalSop.AccountPeriod?.InitialDate ?? DateTime.MinValue,
					GlobalOrder = originalSop.AccountPeriod.Account.Position ?? 0,
					PossibleDateRange = dateRange,
					SpendInfo = spendViewModel,
					SuggestedDate = DateTime.UtcNow,
					SpendTypeViewModels = spendTypes.Select(sp => sp.ToSpendTypeViewModel(spend.SpendTypeId)),
					SupportedAccountInclude = accountIncludeViewModels,
					SupportedCurrencies = [currency],
					TransferInfo = CreateBasicTransferInfo(spend)
				}
			];
		}

		public async Task<IEnumerable<CurrencyViewModel>> GetPossibleCurrenciesAsync(int accountId, string userId)
		{
			var account = await Context.Account.AsNoTracking()
				.FirstAsync(acc => acc.AccountId == accountId);
			var ccms = await Context.CurrencyConverterMethod.AsNoTracking()
				.Include(x => x.CurrencyConverter)
					.ThenInclude(x => x.CurrencyOne)
				.Where(x =>
					(x.FinancialEntityId == account.FinancialEntityId || x.IsDefault.Value)
					&& x.CurrencyConverter.CurrencyTwo.CurrencyId == account.CurrencyId)
				.Select(x => new CurrencyViewModel
				{
					AccountId = account.AccountId,
					CurrencyId = x.CurrencyConverter.CurrencyOne.CurrencyId,
					CurrencyName = x.CurrencyConverter.CurrencyOne.Name,
					Symbol = x.CurrencyConverter.CurrencyOne.Symbol,
					MethodIds = new[]
					{
						new MethodId
						{
							Id = x.CurrencyConverterMethodId,
							Name = x.Name,
							IsDefault = x.IsDefault ?? false,
							IsSelected = x.IsDefault ?? false
						}
					},
					IsDefault = x.IsDefault ?? false,
				})
				.ToListAsync();

			return ccms;
		}

		public async Task<IEnumerable<SavedSpend>> GetSavedSpendsAsync(int spendId)
		{
			var spends = new List<Spend>()
			{
				await Context.Spend.AsNoTracking()
					.Where(sp=>sp.SpendId == spendId)
					.Include(sp=>sp.SpendOnPeriod)
						.ThenInclude(sop => sop.AccountPeriod)
					.FirstAsync()
			};

			var transferSpends = await GetTransferRelatedAppTrxs([spendId], true);
			if(transferSpends.Count != 0)
			{
				spends.AddRange(transferSpends);
			}

			var saveSpends = spends.Select(CreateSavedSpend);
			return saveSpends;
		}

		public async Task<SpendActionAttributes> GetSpendAttributesAsync(int spendId)
		{
			var isLoan = await IsLoanSpendDependentAsync([spendId]);
			var isTransfer = await Context.TransferRecord.AnyAsync(tr => tr.SpendId == spendId);
			return new SpendActionAttributes
			{
				SpendId = spendId,
				IsLoan = isLoan,
				IsTransfer = isTransfer
			};
		}

		public void RollbackTransaction()
		{
			Context.Database.RollbackTransaction();
		}

		public void BeginTransaction()
		{
			Context.Database.BeginTransaction();
		}

		public void Commit()
		{
			Context.Database.CommitTransaction();
		}

		#endregion

		private static BasicTransferInfo CreateBasicTransferInfo(Spend spend)
		{
			ArgumentNullException.ThrowIfNull(spend);
			if(spend.SourceAppTransfer == null && spend.DestinationAppTransfer == null)
			{
				return null;
			}

			var transfer = spend.SourceAppTransfer ?? spend.DestinationAppTransfer;
			var incomeAccountName = transfer.DestinationAppTrx.SpendOnPeriod.First(sop => sop.IsOriginal == true).AccountPeriod.Account.Name;
			var spendAccountName = transfer.SourceAppTrx.SpendOnPeriod.First(sop => sop.IsOriginal == true).AccountPeriod.Account.Name;
			return new BasicTransferInfo
			{
				DestinationAccountName = incomeAccountName,
				SourceAccountName = spendAccountName,
			};
		}
		private async Task<bool> IsLoanSpendDependentAsync(IReadOnlyCollection<int> requestedSpendIds)
		{
			var dependencies = await GetSpendDependenciesAsync(requestedSpendIds);
			var spendIds = dependencies.Select(x => x.SpendId).ToList();
			spendIds.AddRange(requestedSpendIds);
			return await Context.LoanRecord.AnyAsync(lr => spendIds.Contains(lr.SpendId))
				|| await Context.LoanSpend.AnyAsync(ls => spendIds.Contains(ls.SpendId));
		}

		private static DateRange GetDateRange(IEnumerable<Models.AccountPeriod> accountPeriods, DateTime? validateDate)
		{
			if (accountPeriods == null || !accountPeriods.Any())
			{
				throw new ArgumentException(nameof(accountPeriods));
			}

			var lowest = accountPeriods.First().InitialDate;
			var highest = accountPeriods.First().EndDate;
			foreach (var accountPeriod in accountPeriods)
			{
				if (accountPeriod.InitialDate > lowest)
				{
					lowest = accountPeriod.InitialDate;
				}

				if (accountPeriod.EndDate < highest)
				{
					highest = accountPeriod.EndDate;
				}
			}

			return new DateRange
			{
				EndDate = highest,
				StartDate = lowest,
				IsValid = highest > lowest,
				ActualDate = validateDate,
				IsDateValid = validateDate != null
					? (bool?)(validateDate.Value >= lowest && validateDate.Value < highest)
					: null
			};
		}

		private static IEnumerable<CurrencyViewModel> CreateCurrencyViewModelFromMethods(IEnumerable<CurrencyConverterMethod> currencyConverterMethods, Account account)
		{
			var currencies = new List<CurrencyViewModel>();
			foreach (var ccm in currencyConverterMethods)
			{
				var ccmCurrency = ccm.CurrencyConverter.CurrencyOne;
				var currency = currencies.FirstOrDefault(c => c.CurrencyId == ccm.CurrencyConverter.CurrencyIdOne);
				if (currency == null)
				{
					var defaultCurrencyId = account.DefaultSelectCurrencyId != null ? account.DefaultSelectCurrencyId : account.CurrencyId;
					currency = new CurrencyViewModel
					{
						CurrencyId = ccmCurrency.CurrencyId,
						AccountId = account.AccountId,
						CurrencyName = ccmCurrency.Name,
						MethodIds = new List<MethodId>(),
						Symbol = ccmCurrency.Symbol,
						IsDefault = ccmCurrency.CurrencyId == defaultCurrencyId
					};

					currencies.Add(currency);
				}

				var methodsList = (List<MethodId>)currency.MethodIds;
				methodsList.Add(new MethodId
				{
					Id = ccm.CurrencyConverterMethodId,
					IsDefault = ccm.IsDefault ?? false,
					Name = ccm.Name
				});
			}

			return currencies;
		}

		private static SavedSpend CreateSavedSpend(Spend spend)
		{
			var saveSpend = new SavedSpend
			{
				Amount = IntExt.ToFloat(spend.OriginalAmount),
				AmountDenominator = IntExt.ToNullFloat(spend.Denominator),
				AmountNumerator = IntExt.ToNullFloat(spend.Numerator),
				AmountTypeId = (TransactionTypeIds)spend.AmountTypeId,
				CurrencyId = spend.AmountCurrencyId ?? 0,
				Description = spend.Description,
				IsPending = spend.IsPending,
				SpendId = spend.SpendId,
				IncludedAccounts = new List<ClientAddSpendAccount>(),
				SpendDate = spend.SpendDate ?? new DateTime(),
				SpendTypeId = spend.SpendTypeId ?? 0,
				IsPurchase = spend.IsPurchase,
				MethodId = spend.CurrencyConverterMethodId
			};

			foreach (var spendOnPeriod in spend.SpendOnPeriod)
			{
				var addSpendAccount = new ClientAddSpendAccount
				{
					AccountId = spendOnPeriod.AccountPeriod.AccountId ?? 0,
					ConvertionMethodId = spendOnPeriod.CurrencyConverterMethodId ?? 0
				};

				if (spendOnPeriod.IsOriginal != null && spendOnPeriod.IsOriginal.Value)
				{
					saveSpend.OriginalAccountData = addSpendAccount;
				}
				else
				{
					((IList<ClientAddSpendAccount>)saveSpend.IncludedAccounts).Add(addSpendAccount);
				}
			}

			return saveSpend;
		}

		private async Task AddSpendDependencyAsync(int spendId, int dependencySpendId, bool twoWay)
		{
			var addItems = new List<SpendDependencies>
			{
				new SpendDependencies
				{
					DependencySpendId = dependencySpendId,
					SpendId = spendId
				}
			};

			if (twoWay)
			{
				addItems.Add(new SpendDependencies
				{
					DependencySpendId = spendId,
					SpendId = dependencySpendId
				});
			}

			await Context.SpendDependencies.AddRangeAsync(addItems);
			await Context.SaveChangesAsync();
		}

		private async Task<IReadOnlyCollection<Spend>> GetSpendDependenciesAsync(IReadOnlyCollection<int> spendIds)
		{
			var dependencies = new List<Spend>();
			var trasnferIds = await Context.TransferRecord.AsNoTracking()
				.Where(tr => spendIds.Contains(tr.SpendId))
				.Select(tr => tr.TransferRecordId)
				.ToListAsync();
			if (trasnferIds.Count != 0)
			{
				var transferDeps = await Context.TransferRecord.AsNoTracking()
						.Where(t => trasnferIds.Contains(t.TransferRecordId))
						.Select(t => t.Spend)
						.Where(sp => !spendIds.Contains(sp.SpendId))
						.ToListAsync();
				dependencies.AddRange(transferDeps);
			}

			var withTransferSpends = await GetTransferRelatedAppTrxs(spendIds, true);
			if(withTransferSpends.Any())
			{
				dependencies.AddRange(withTransferSpends);
			}

			var loanId = await Context.LoanRecord.AsNoTracking()
				.Where(lr => spendIds.Contains(lr.SpendId))
				.Select(lr => lr.LoanRecordId)
				.FirstOrDefaultAsync();
			if (loanId > 0)
			{
				var loanTrxs = await Context.LoanSpend.AsNoTracking()
					.Where(ls => ls.LoanRecordId == loanId)
					.Include(ls => ls.Spend)
					.Select(ls => ls.Spend)
					.ToListAsync();
				dependencies.AddRange(loanTrxs);
			}

			var allEvaluate = await Context.Spend.AsNoTracking()
				.Where(sp => spendIds.Contains(sp.SpendId))
				.ToListAsync();
			allEvaluate.AddRange(dependencies);
			dependencies.AddRange(await GetDependenciesRecursivleyAsync(allEvaluate));
			return dependencies;
		}

		private async Task<bool> AppTrxsHasTransfersAsync(IReadOnlyCollection<int> spendIds)
		{
			return await Context.Spend.AsNoTracking()
				.Where(sp => spendIds.Contains(sp.SpendId) && (sp.SourceAppTransfer != null || sp.DestinationAppTransfer != null))
				.AnyAsync();
		}

		private async Task<IReadOnlyCollection<Spend>> GetTransferRelatedAppTrxs(IReadOnlyCollection<int> spendIds, bool asReadOnly)
		{
			if (spendIds == null || spendIds.Count == 0)
			{
				return [];
			}

			var dependencies = new List<Spend>();
			IQueryable<Spend> query = Context.Spend;
			if (asReadOnly)
			{
				query = query.AsNoTracking();
			}
			var withTransferSpends = await query
				.Where(sp => spendIds.Contains(sp.SpendId) && (sp.SourceAppTransfer != null || sp.DestinationAppTransfer != null))
				.Include(sp => sp.SourceAppTransfer)
					.ThenInclude(tr => tr.DestinationAppTrx)
						.ThenInclude(spd => spd.SpendOnPeriod)
							.ThenInclude(sop => sop.AccountPeriod)
				.Include(sp => sp.DestinationAppTransfer)
					.ThenInclude(tr => tr.SourceAppTrx)
						.ThenInclude(spd => spd.SpendOnPeriod)
							.ThenInclude(sop => sop.AccountPeriod)
				.ToListAsync();
			if (withTransferSpends.Count != 0)
			{
				var transferDeps = withTransferSpends
					.SelectMany(sp =>
					{
						var deps = new List<Spend>();
						if (sp.SourceAppTransfer != null)
						{
							deps.Add(sp.SourceAppTransfer.DestinationAppTrx);
						}

						if (sp.DestinationAppTransfer != null)
						{
							deps.Add(sp.DestinationAppTransfer.SourceAppTrx);
						}

						return deps;
					});
				dependencies.AddRange(transferDeps);
			}

			return dependencies;
		}

		private async Task<IReadOnlyCollection<Spend>> GetDependenciesRecursivleyAsync(IReadOnlyCollection<Spend> spends)
		{
			var resultList = new List<Spend>();
			var evaluateList = spends.ToList();
			while (evaluateList.Any())
			{
				var evaluateElement = evaluateList.First();
				evaluateList.Remove(evaluateElement);
				var firstLevelDeps = await GetFirstLevelDependenciesAsync(evaluateElement.SpendId);
				if (firstLevelDeps != null && firstLevelDeps.Any())
				{
					evaluateList.AddRange(firstLevelDeps);
					resultList.AddRange(firstLevelDeps);
				}
			}

			return resultList;
		}

		private async Task<IReadOnlyCollection<Spend>> GetFirstLevelDependenciesAsync(int spendId)
		{
			return await Context.SpendDependencies.AsNoTracking()
				.Where(sd => sd.SpendId == spendId)
				.Include(sd => sd.DependencySpend)
				.Select(sd => sd.DependencySpend)
				.ToListAsync();
		}

		private async Task ValidateSpendIdInLoanAsync(IReadOnlyCollection<int> spendIds)
		{
			var existsInLoan = await Context.LoanRecord
				.Where(lr => spendIds.Contains(lr.SpendId)).AnyAsync();
			if (existsInLoan)
			{
				throw new Exception("Not allowed to delete loan record spend");
			}

			if (await Context.SpendDependencies
				.Include(spd => spd.Spend)
					.ThenInclude(sp => sp.LoanRecord)
				.AnyAsync(spd => spendIds.Contains(spd.DependencySpendId) && spd.Spend.LoanRecord != null))
			{
				throw new Exception("Not allowed to delete loan record spend");
			}
		}

		private void ValidateEitherOrAccountIdValues(int? id1, int? id2)
		{

			if ((id1 == null || id1 == 0) && (id2 == null || id2 == 0))
			{
				throw new AggregateException(new ArgumentException(@"Both parameters cannot be empty",
					nameof(id1)));
			}

			if ((id1 != null && id1 != 0) && (id2 != null && id2 != 0))
			{
				throw new AggregateException(new ArgumentException(@"Only one parameters can be specified",
					nameof(id1)));
			}
		}

		private async Task<IReadOnlyCollection<AccountFinanceViewModel>> GetAccountFinanceViewModelAsync(
			IReadOnlyCollection<ClientAccountFinanceViewModel> requestItems,
			DateTime? currentDate
			)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			if (requestItems == null || !requestItems.Any())
			{
				return Array.Empty<AccountFinanceViewModel>();
			}

			var requiresLoan = requestItems.Any(r => r.LoanSpends);
			var accountPeriodIds = requestItems.Select(accp => accp.AccountPeriodId);
			var infoIds = await Context.AccountPeriod.AsNoTracking()
				.Where(acc => accountPeriodIds.Contains(acc.AccountPeriodId))
				.Select(accp => new { accp.AccountId, accp.AccountPeriodId })
				.ToListAsync();
			var accountIds = infoIds.Select(acc => acc.AccountId);
			IQueryable<Account> query = Context.Account.AsNoTracking()
				.Where(acc => accountIds.Contains(acc.AccountId))
				.Include(acc => acc.Currency)
				.Include(acc => acc.AccountPeriod)
					.ThenInclude(accp => accp.SpendOnPeriod)
						.ThenInclude(sop => sop.Spend)
							.ThenInclude(sp => sp.AmountCurrency)
				.Include(acc => acc.AccountPeriod)
					.ThenInclude(accp => accp.SpendOnPeriod)
						.ThenInclude(sop => sop.Spend)
							.ThenInclude(sp => sp.SpendType);
			if (requiresLoan)
			{
				query = query
					.Include(acc => acc.AccountPeriod)
						.ThenInclude(accp => accp.SpendOnPeriod)
							.ThenInclude(sop => sop.Spend)
								.ThenInclude(sp => sp.LoanRecord);
			}

			var accViewModels = new List<AccountFinanceViewModel>();
			var accounts = await query.ToListAsync();
			foreach (var account in accounts)
			{
				var accInfo = infoIds.First(x => x.AccountId == account.AccountId);
				var currentAccountPeriod = AccountHelpers.GetCurrentAccountPeriod(currentDate, account);
				var selectedAccountPeriod = account.AccountPeriod.First(accp => accp.AccountPeriodId == accInfo.AccountPeriodId);
				var requestParams = requestItems.First(r => r.AccountPeriodId == selectedAccountPeriod.AccountPeriodId);

				var periodsSumResult = SumPeriods(account.AccountPeriod, requestParams,
					currentAccountPeriod?.AccountPeriodId, selectedAccountPeriod.AccountPeriodId);
				var periodBudget = selectedAccountPeriod.Budget ?? 0;
				var viewModel = new AccountFinanceViewModel
				{
					AccountId = account.AccountId,
					AccountName = account.Name,
					AccountPeriodId = accInfo.AccountPeriodId,
					Budget = periodBudget,
					CurrencyId = account.CurrencyId ?? 0,
					CurrencySymbol = account.Currency.Symbol,
					EndDate = selectedAccountPeriod.EndDate ?? new DateTime(),
					GlobalOrder = account.Position ?? 0,
					InitialDate = selectedAccountPeriod.InitialDate ?? new DateTime(),
					SpendViewModels = periodsSumResult.SelectedPeriodSum.SpendViewModels,
					Spent = (float)(periodsSumResult.SelectedPeriodSum.BalanceSum),
					GeneralBalance = (float)(periodsSumResult.NotCurrentTotalBudgetSum - periodsSumResult.NotCurrentTotalTrxSum),
					GeneralBalanceToday = (float)(periodsSumResult.CurrentIncludedBudgetSum - periodsSumResult.CurrentIncludedTrxSum),
					PeriodBalance = (float)(periodBudget - periodsSumResult.SelectedPeriodSum.BalanceSum),
					TrxFilters = requestParams.TrxFilters
				};

				accViewModels.Add(viewModel);
			}

			stopwatch.Stop();
			_logger.LogDebug($"Finance Info took {stopwatch.Elapsed:g}");
			return accViewModels;
		}

		private static AccountPeriodsSumRes SumPeriods(
			ICollection<Models.AccountPeriod> accountPeriods
			, ClientAccountFinanceViewModel requestParams
			, int? currentPeriodId
			, int selectedPeriodId
			)
		{
			return requestParams.TrxFilters != null
				? SumPeriodsByFilters(accountPeriods, requestParams)
				: SumPeriodsBySelectedPeriod(accountPeriods, requestParams, currentPeriodId, selectedPeriodId);
		}

		private static AccountPeriodsSumRes SumPeriodsByFilters(ICollection<Models.AccountPeriod> accountPeriods, ClientAccountFinanceViewModel requestParams)
		{
			var sumResult = new AccountPeriodsSumRes();
			if (accountPeriods == null || !accountPeriods.Any())
			{
				return sumResult;
			}

			foreach (var accountPeriod in accountPeriods)
			{
				var trxSumResult = GetTrxSumResult(accountPeriod.SpendOnPeriod, requestParams, false);
				sumResult.CurrentIncludedTrxSum += trxSumResult.BalanceSum;
				sumResult.CurrentIncludedBudgetSum += accountPeriod.Budget ?? 0;
				sumResult.SelectedPeriodSum.SpendViewModels.AddRange(trxSumResult.SpendViewModels);
			}

			return sumResult;
		}

		private static AccountPeriodsSumRes SumPeriodsBySelectedPeriod(
			ICollection<Models.AccountPeriod> accountPeriods
			, ClientAccountFinanceViewModel requestParams
			, int? currentPeriodId
			, int selectedPeriodId
			)
		{
			var sumResult = new AccountPeriodsSumRes();
			if (accountPeriods == null || !accountPeriods.Any())
			{
				return sumResult;
			}

			foreach (var accountPeriod in accountPeriods)
			{
				var isCurrentPeriod = accountPeriod.AccountPeriodId == currentPeriodId;
				var isSelectedPeriod = accountPeriod.AccountPeriodId == selectedPeriodId;
				var trxSumResult = GetTrxSumResult(accountPeriod.SpendOnPeriod, requestParams, !isSelectedPeriod);
				if (isSelectedPeriod)
				{
					sumResult.SelectedPeriodSum = trxSumResult;
				}

				sumResult.CurrentIncludedTrxSum += trxSumResult.BalanceSum;
				sumResult.CurrentIncludedBudgetSum += accountPeriod.Budget ?? 0;
				if (!isCurrentPeriod)
				{
					sumResult.NotCurrentTotalTrxSum += trxSumResult.BalanceSum;
					sumResult.NotCurrentTotalBudgetSum += accountPeriod.Budget ?? 0;
				}
			}

			return sumResult;
		}

		private static TrxSumResult GetTrxSumResult(ICollection<SpendOnPeriod> spendOnPeriods, ClientAccountFinanceViewModel requestParams, bool ignoreTrxViewModels)
		{
			var sumResult = new TrxSumResult();
			foreach (var spendOnPeriod in spendOnPeriods.Where(sop => FilterSpend(sop.Spend, requestParams)))
			{
				if (!ignoreTrxViewModels)
				{
					var spendViewModel = spendOnPeriod.ToSpendSpendViewModel<FinanceSpendViewModel>();
					sumResult.SpendViewModels.Add(spendViewModel);
				}

				sumResult.BalanceSum += spendOnPeriod.GetAmount();
			}

			return sumResult;
		}

		private static bool FilterSpend(Spend spend, ClientAccountFinanceViewModel request)
		{
			if (request.TrxFilters != null)
			{
				return FilterSpend(spend, request.TrxFilters);
			}
			else
			{
				if (!request.PendingSpends && spend.IsPending)
				{
					return false;
				}

				if (!request.LoanSpends && spend.LoanRecord != null)
				{
					return false;
				}

				return request.AmountTypeId == 0 || spend.AmountTypeId == request.AmountTypeId;
			}
		}

		private static bool FilterSpend(Spend spend, TrxFiltersContainer trxFiltersContainer)
		{
			if (trxFiltersContainer.StartDate != null && (spend.SpendDate == null || spend.SpendDate < trxFiltersContainer.StartDate))
			{
				return false;
			}

			if (trxFiltersContainer.EndDate != null && (spend.SpendDate == null || spend.SpendDate > trxFiltersContainer.EndDate))
			{
				return false;
			}

			if (trxFiltersContainer.PendingTrxFilter != null && trxFiltersContainer.PendingTrxFilter.Value && !spend.IsPending)
			{
				return false;
			}

			if (trxFiltersContainer.DescriptionTrxFilter != null &&
				(string.IsNullOrWhiteSpace(spend.Description) || !spend.Description.Contains(trxFiltersContainer.DescriptionTrxFilter.SearchText)))
			{
				return false;
			}

			return true;
		}

		private async Task<IEnumerable<AddSpendAccountDbValues>> GetConvertedAccountIncludedAsync(ISpendCurrencyConvertible spendCurrencyConvertible)
		{
			var accountIds = SpendsDataHelper.GetInvolvedAccountIds(spendCurrencyConvertible);
			var accountCurrencyPairList = await GetAccountsCurrencyAsync(accountIds);
			return await _trxExchangeService.ConvertTrxCurrencyAsync(spendCurrencyConvertible, accountCurrencyPairList.ToList());
		}

		private async Task ValidateSpendCurrencyConvertibleValuesAsync(ISpendCurrencyConvertible spendCurrencyConvertible)
		{
			ArgumentNullException.ThrowIfNull(spendCurrencyConvertible);
			var accountData =
				spendCurrencyConvertible.IncludedAccounts.Select(
					item => SpendsDataHelper.CreateClientAddSpendCurrencyData(item, spendCurrencyConvertible.CurrencyId));
			var clientAddSpendValidationResultSet = await GetClientAddSpendValidationResultSetAsync(accountData);
			var invalids = clientAddSpendValidationResultSet.Where(item => !item.IsSuccess);
			if (!invalids.Any())
				return;
			var invalidAccounts = invalids.Select(SpendsDataHelper.CreateAccountCurrencyConverterData);
			throw new InvalidAddSpendCurrencyException(invalidAccounts);
		}

		private async Task<IEnumerable<ClientAddSpendValidationResultSet>> GetClientAddSpendValidationResultSetAsync(
			IEnumerable<ClientAddSpendCurrencyData> clientAddSpendCurrencyDataList)
		{
			var accountIds = clientAddSpendCurrencyDataList.Select(c => c.AccountId);
			var accounts = await Context.Account
				.Where(acc => accountIds.Contains(acc.AccountId))
				.ToListAsync();
			var ccmIds = clientAddSpendCurrencyDataList.Select(ccm => ccm.CurrencyConverterMethodId);
			var currencyConverterMethods = await Context.CurrencyConverterMethod
				.Where(ccm => ccmIds.Contains(ccm.CurrencyConverterMethodId))
				.Include(ccm => ccm.CurrencyConverter)
				.ToListAsync();
			var results = new List<ClientAddSpendValidationResultSet>();
			foreach (var cAccount in clientAddSpendCurrencyDataList)
			{
				var account = accounts.First(acc => acc.AccountId == cAccount.AccountId);
				var ccm = currencyConverterMethods.First(x => x.CurrencyConverterMethodId == cAccount.CurrencyConverterMethodId);
				var result = new ClientAddSpendValidationResultSet
				{
					AccountId = account.AccountId,
					AccountName = account.Name,
					AmountCurrencyId = cAccount.AmountCurrencyId,
					ConvertCurrencyId = ccm.CurrencyConverterId,
					CurrencyIdOne = ccm.CurrencyConverter.CurrencyIdOne,
					CurrencyIdTwo = ccm.CurrencyConverter.CurrencyIdTwo,
					IsSuccess = ccm.CurrencyConverter.CurrencyIdOne == cAccount.AmountCurrencyId
						&& ccm.CurrencyConverter.CurrencyIdTwo == account.CurrencyId
				};

				results.Add(result);
			}

			return results;
		}

		private static class IntExt
		{
			public static float ToFloat(double? value)
			{
				return value != null ? (float)value.Value : 0f;
			}

			public static float? ToNullFloat(double? value)
			{
				return value != null ? (float)value.Value : (float?)null;
			}
		}

		private class AccountPeriodsSumRes
		{

			public double NotCurrentTotalBudgetSum { get; set; }
			public double NotCurrentTotalTrxSum { get; set; }
			public double CurrentIncludedTrxSum { get; set; }
			public double CurrentIncludedBudgetSum { get; set; }

			public TrxSumResult SelectedPeriodSum { get; set; } = new TrxSumResult();
		}

		private class TrxSumResult
		{
			public double BalanceSum { get; set; }
			public List<FinanceSpendViewModel> SpendViewModels { get; set; } = new List<FinanceSpendViewModel>();
		}
	}
}
