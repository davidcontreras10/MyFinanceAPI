using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	public class SpendsService(IUnitOfWork unitOfWork, IAppTransactionsSubService appTransactionsSubService) : ISpendsService
	{

		#region Attributes

		private readonly ISpendsRepository _spendsRepository = unitOfWork.SpendsRepository;
		private readonly IResourceAccessRepository _resourceAccessRepository = unitOfWork.ResourceAccessRepository;

		#endregion

		#region Public Methods

		public async Task<SpendActionResult> GetSpendActionResultAsync(int spendId, ResourceActionNames actionType, ApplicationModules applicationModule)
		{
			if (actionType == ResourceActionNames.Unknown)
			{
				throw new ArgumentException(nameof(actionType));
			}

			if (applicationModule == ApplicationModules.Unknown)
			{
				throw new ArgumentException(nameof(applicationModule));
			}

			var spendAttributes = await _spendsRepository.GetSpendAttributesAsync(spendId);
			var applicationResource = ApplicationResources.Spends;
			var resourcesAccessResponse = await _resourceAccessRepository.GetResourceAccessReportAsync(applicationResourceId: (int)applicationResource,
				applicationModuleId: (int)applicationModule, resourceActionId: (int)actionType, resourceAccessLevelId: null);
			var response = CreateSpendActionResult(spendAttributes, resourcesAccessResponse, actionType);
			return response;
		}

		public async Task<IEnumerable<AddSpendViewModel>> GetAddSpendViewModelAsync(IEnumerable<int> accountPeriodIds, string userId)
		{
			return await _spendsRepository.GetAddSpendViewModelAsync(accountPeriodIds, userId);
		}

		public async Task<IEnumerable<EditSpendViewModel>> GetEditSpendViewModelAsync(int accountPeriodId, int spendId, string userId)
		{
			return await _spendsRepository.GetEditSpendViewModelAsync(accountPeriodId, spendId, userId);
		}

		public async Task<IEnumerable<SavedSpend>> GetSavedSpendsAsync(int spendId)
		{
			return await _spendsRepository.GetSavedSpendsAsync(spendId);
		}

		public async Task<IEnumerable<SpendItemModified>> AddNewAppTransactionByAccountAsync(NewAppTransactionByAccount newAppTransactionByAccount)
		{
			var period = await unitOfWork.AccountRepository.GetAccountPeriodInfoByAccountIdDateTimeAsync(newAppTransactionByAccount.AccountId, newAppTransactionByAccount.SpendDate) 
				?? throw new ArgumentException("Period not found");
			var clientAddSpendModel = new ClientBasicTrxByPeriod
			{
				Amount = newAppTransactionByAccount.Amount,
				AmountTypeId = newAppTransactionByAccount.TransactionType,
				CurrencyId = newAppTransactionByAccount.CurrencyId,
				SpendDate = newAppTransactionByAccount.SpendDate,
				UserId = newAppTransactionByAccount.UserId,
				AccountPeriodId = period.AccountPeriodId,
				Description = newAppTransactionByAccount.Description,
				IsPending = newAppTransactionByAccount.IsPending,
				SpendTypeId = newAppTransactionByAccount.SpendTypeId,

			};

			return await AddBasicTransactionAsync(clientAddSpendModel, newAppTransactionByAccount.TransactionType);
		}

		public async Task<IEnumerable<SpendItemModified>> AddBasicTransactionAsync(ClientBasicTrxByPeriod clientBasicTrxByPeriod, TransactionTypeIds transactionTypeId)
		{
			if (clientBasicTrxByPeriod.Amount <= 0)
				throw new InvalidAmountException();

			if (transactionTypeId == TransactionTypeIds.Invalid || transactionTypeId == TransactionTypeIds.Ignore)
			{
				throw new ArgumentException($"{nameof(transactionTypeId)} cannot be invalid");
			}

			var clientAddSpendModel = await
				_spendsRepository.CreateClientAddSpendModelAsync(clientBasicTrxByPeriod,
					clientBasicTrxByPeriod.AccountPeriodId);
			return transactionTypeId == TransactionTypeIds.Saving
				? await AddIncomeAsync(clientAddSpendModel)
				: await AddSpendAsync(clientAddSpendModel);
		}

		public async Task<IEnumerable<SpendItemModified>> AddIncomeAsync(ClientAddSpendModel clientAddSpendModel)
		{
			if (clientAddSpendModel.Amount <= 0)
				throw new InvalidAmountException();
			clientAddSpendModel.AmountTypeId = TransactionTypeIds.Saving;
			var result = await appTransactionsSubService.AddMultipleTransactionsAsync(new[] { clientAddSpendModel });
			return result.Select(ToSpendItemModified);
		}

		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel)
		{
			ArgumentNullException.ThrowIfNull(clientAddSpendModel);
			if (clientAddSpendModel.Amount <= 0)
				throw new ArgumentException("Amount must be greater than zero");
			clientAddSpendModel.AmountTypeId = TransactionTypeIds.Spend;
			var result = await appTransactionsSubService.AddMultipleTransactionsAsync(new[] { clientAddSpendModel });
			return result.Select(ToSpendItemModified);
		}

		public async Task<IEnumerable<SpendItemModified>> DeleteSpendAsync(string userId, IReadOnlyCollection<int> transactionIds)
		{
			var trxWithBankTrx = await _spendsRepository.AppTransactionsWithBankTrxAsync(transactionIds);
			if (trxWithBankTrx.Count != 0)
			{
				throw new ServiceException(AppErrorCodes.DeleteTrxWithBankTrx);		
			}

			var result = await _spendsRepository.DeleteTransactionsAsync(transactionIds);
			await unitOfWork.SaveAsync();
			return result;
		}

		public async Task<IEnumerable<SpendItemModified>> EditSpendAsync(ClientEditSpendModel model)
		{
			return await _spendsRepository.EditSpendAsync(model);
		}

		public async Task<IEnumerable<AccountCurrencyPair>> GetAccountsCurrencyAsync(IEnumerable<int> accountIdsArray)
		{
			var result = await _spendsRepository.GetAccountsCurrencyAsync(accountIdsArray);
			return result;
		}

		public async Task<IEnumerable<SpendItemModified>> ConfirmPendingTransactionsAsync(IReadOnlyCollection<int> transactionIds, DateTime newPaymentDate)
		{
			if (transactionIds == null || !transactionIds.Any())
			{
				return Array.Empty<SpendItemModified>();
			}

			_spendsRepository.BeginTransaction();
			var modifiedList = new List<SpendItemModified>();
			try
			{
				//await Parallel.ForEachAsync(transactionIds, async (int trxId, CancellationToken cancellationToken) =>
				//{
				//	var modifieds = await ExecuteConfirmPendingTransactionAsync(trxId, newPaymentDate);
				//	foreach (var modified in modifieds)
				//	{
				//		modifiedList.Add(modified);
				//	}
				//});

				foreach (var transactionId in transactionIds)
				{
					var modifieds = await ExecuteConfirmPendingTransactionAsync(transactionId, newPaymentDate);
					var notIncluded = modifieds.Where(m => !modifiedList.Any(mli => mli == m));
					modifiedList.AddRange(notIncluded);
				}

				_spendsRepository.Commit();
				return modifiedList;
			}
			catch (Exception ex)
			{
				_spendsRepository.RollbackTransaction();
				throw;
			}

		}

		public async Task<IEnumerable<SpendItemModified>> ConfirmPendingSpendAsync(int spendId, DateTime newPaymentDate)
		{
			_spendsRepository.BeginTransaction();
			try
			{
				var modifiedList = await ExecuteConfirmPendingTransactionAsync(spendId, newPaymentDate);
				_spendsRepository.Commit();
				return modifiedList;
			}
			catch (Exception)
			{
				_spendsRepository.RollbackTransaction();
				throw;
			}
		}

		#endregion

		#region Privates

		private async Task<IEnumerable<SpendItemModified>> ExecuteConfirmPendingTransactionAsync(int spendId, DateTime newPaymentDate)
		{
			var spends = await _spendsRepository.GetSavedSpendsAsync(spendId);
			if (spends == null || !spends.Any())
			{
				return [];
			}
			var modifiedList = new List<SpendItemModified>();
			foreach (var savedSpend in spends)
			{
				var financeSpend = CreateFinanceSpend(savedSpend, newPaymentDate);
				if (!savedSpend.IsPending)
				{
					throw new SpendNotPendingException(financeSpend.SpendId);
				}

				var modifiedItems = await _spendsRepository.EditSpendAsync(financeSpend);
				modifiedList.AddRange(modifiedItems);
			}

			return modifiedList;
		}

		private static FinanceSpend CreateFinanceSpend(SavedSpend savedSpend, DateTime newDateTime)
		{
			ArgumentNullException.ThrowIfNull(savedSpend);
			var result = new FinanceSpend
			{
				SpendId = savedSpend.SpendId,
				Amount = savedSpend.Amount,
				UserId = savedSpend.UserId,
				SpendDate = savedSpend.SpendDate,
				AmountDenominator = savedSpend.AmountDenominator,
				CurrencyId = savedSpend.CurrencyId,
				AmountNumerator = savedSpend.AmountNumerator,
				SetPaymentDate = newDateTime,
				OriginalAccountData = savedSpend.OriginalAccountData,
				IncludedAccounts = savedSpend.IncludedAccounts,
				IsPending = false,
				AmountTypeId = savedSpend.AmountTypeId
			};

			return result;
		}

		private static SpendActionResult CreateSpendActionResult(SpendActionAttributes spendActionAttributes,
			IEnumerable<ResourceAccessReportRow> resourceAccessReportRows, ResourceActionNames resourceActionNames)
		{
			ArgumentNullException.ThrowIfNull(spendActionAttributes);
			ArgumentNullException.ThrowIfNull(resourceAccessReportRows);

			var response = new SpendActionResult
			{
				Action = resourceActionNames,
				IsLoan = spendActionAttributes.IsLoan,
				IsTransfer = spendActionAttributes.IsTransfer,
				SpendId = spendActionAttributes.SpendId
			};

			var isValid = resourceAccessReportRows.Any(r =>
				spendActionAttributes.AccessLevels.Any(r2 => (int)r2 == r.ResourceAccessLevelId));
			if (isValid)
			{
				response.Result = SpendActionAttributes.ActionResult.Valid;
				return response;
			}

			response.Result = SpendActionAttributes.ActionResult.Unknown;
			return response;
		}

		private static SpendItemModified ToSpendItemModified(TrxItemModifiedRecord trxItemModifiedRecord)
		{
			return new SpendItemModified
			{
				AccountId = trxItemModifiedRecord.AccountId,
				IsModified = trxItemModifiedRecord.IsModified,
				SpendId = trxItemModifiedRecord.SpendId
			};
		}

		#endregion
	}
}