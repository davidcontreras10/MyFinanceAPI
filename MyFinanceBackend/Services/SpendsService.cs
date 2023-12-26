using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceBackend.Models;
using MyFinanceBackend.ServicesExceptions;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
	public class SpendsService : ISpendsService
	{
		#region Constructor

		public SpendsService(ISpendsRepository spendsRepository, IResourceAccessRepository resourceAccessRepository)
		{
			_spendsRepository = spendsRepository;
			_resourceAccessRepository = resourceAccessRepository;
		}

		#endregion

		#region Attributes

		private readonly ISpendsRepository _spendsRepository;
		private readonly IResourceAccessRepository _resourceAccessRepository;

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
			var result = await _spendsRepository.AddSpendAsync(clientAddSpendModel);
			return result;
		}

		public async Task<IEnumerable<SpendItemModified>> AddSpendAsync(ClientAddSpendModel clientAddSpendModel)
		{
			if (clientAddSpendModel == null)
				throw new ArgumentNullException(nameof(clientAddSpendModel));
			if (clientAddSpendModel.Amount <= 0)
				throw new ArgumentException("Amount must be greater than zero");
			clientAddSpendModel.AmountTypeId = TransactionTypeIds.Spend;
			var result = await _spendsRepository.AddSpendAsync(clientAddSpendModel);
			return result;
		}

		public async Task<IEnumerable<SpendItemModified>> DeleteSpendAsync(string userId, int spendId)
		{
			var result = await _spendsRepository.DeleteSpendAsync(userId, spendId);
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

		public async Task<IEnumerable<SpendItemModified>> ConfirmPendingSpendAsync(int spendId, DateTime newPaymentDate)
		{
			var spends = await _spendsRepository.GetSavedSpendsAsync(spendId);
			if (spends == null || !spends.Any())
			{
				return new SpendItemModified[0];
			}

			var modifiedList = new List<SpendItemModified>();
			_spendsRepository.BeginTransaction();
			try
			{
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
				_spendsRepository.Commit();
			}
			catch (Exception)
			{
				_spendsRepository.RollbackTransaction();
				throw;
			}

			return modifiedList;
		}

		#endregion

		#region Privates

		private static FinanceSpend CreateFinanceSpend(SavedSpend savedSpend, DateTime newDateTime)
		{
			if (savedSpend == null)
			{
				throw new ArgumentNullException(nameof(savedSpend));
			}

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
			if (spendActionAttributes == null)
			{
				throw new ArgumentNullException(nameof(spendActionAttributes));
			}

			if (resourceAccessReportRows == null)
			{
				throw new ArgumentNullException(nameof(resourceAccessReportRows));
			}

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

		#endregion
	}
}