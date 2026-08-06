using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class DebtRequestService(IUnitOfWork unitOfWork, IAppTransactionsSubService appTransactionsSubService, ILogger<DebtRequestService> logger) : IDebtRequestService
	{
		public async Task<UserDebtRequestVm> GetDebtRequestByIdAsync(int debtRequestId, Guid userId)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync(debtRequestId, userId, true) as UserDebtRequestVm
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			return debtRequest;
		}

		public async Task RemoveAppTransactionsFromDebtRequestAsync(int debtRequestId, Guid userId)
		{
			await unitOfWork.DebtRequestRepository.RemoveAllAppTransactionsFromDebtRequestAsync(debtRequestId, userId);
			await unitOfWork.SaveAsync();
		}

		public async Task<IReadOnlyCollection<TrxItemModifiedRecord>> AddAppTransactionsToDebtRequestAsync(
			Guid userId,
			int debtRequestId,
			IReadOnlyCollection<NewDebtRequestAppTrx> newDebtRequestAppTrxes
			)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync(debtRequestId, userId) as UserDebtRequestVm
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			var amountType = debtRequest.CreatedByMe ? TransactionTypeIds.Saving : TransactionTypeIds.Spend;
			var newAppTransactions = CreateNewAppTransactions(userId.ToString(), debtRequest, amountType, newDebtRequestAppTrxes);
			try
			{
				await unitOfWork.StartTransactionAsync();
				var trxItemsModified = await appTransactionsSubService.AddMultipleTrxsByAccountAsync(newAppTransactions);
				var trxIds = trxItemsModified.Select(x => x.SpendId).Distinct().ToList();
				await unitOfWork.DebtRequestRepository.AddAppTransactionAsync(debtRequestId, trxIds, userId);
				await unitOfWork.SaveAsync();
				await unitOfWork.CommitTransactionAsync();
				return [.. trxItemsModified];
			}
			catch
			{
				await unitOfWork.RollbackAsync();
				throw;
			}
		}

		public async Task<UserDebtRequestVm> UpdateCreditorStatusAsync(int debtRequestId, CreditorRequestStatus newStatus, Guid userId, DateTime dateTime)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync<UserDebtRequestVm>(debtRequestId, userId)
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			if(debtRequest.TrxCount == 0)
			{
				debtRequest = await unitOfWork.DebtRequestRepository.UpdateCreditorStatusAsync(debtRequestId, newStatus);
				await unitOfWork.SaveAsync();
				return debtRequest;
			}

			if(newStatus == CreditorRequestStatus.Pending || newStatus == CreditorRequestStatus.Archived)
			{
				return await UpdateCreditorResetStatusAsync(debtRequestId, userId, newStatus);
			}
			else if(newStatus == CreditorRequestStatus.Paid)
			{
				var modifiedTrxs = await ConfirmPendingCreditorDebtRequestTrxsAsync(debtRequestId, userId, dateTime);
				var modifiedDebtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync<TrxModifiedDebtRequestVm>(debtRequestId, userId)
					?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
				modifiedDebtRequest.ModifiedTrxs = modifiedTrxs;
                return modifiedDebtRequest;
			}
			else
			{
				throw new ServiceException("Cannot change creditor status when there are associated transactions. Only reset is allowed.");
			}
		}
		
		private async Task<IEnumerable<SpendItemModified>> ConfirmPendingCreditorDebtRequestTrxsAsync(int debtRequestId, Guid userId, DateTime dateTime)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync<TrxModifiedDebtRequestVm>(debtRequestId, userId, true)
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			if (debtRequest.UserTrxs == null || !debtRequest.UserTrxs.Any())
			{
				throw new ServiceException($"No transactions found for debt request {debtRequestId}");
			}
			var trxIds = debtRequest.UserTrxs.Select(x => x.SpendId).ToList();
			await unitOfWork.StartTransactionAsync();
			try
			{
				var modifieds = await appTransactionsSubService.ExecuteConfirmPendingTransactionsAsync(trxIds, dateTime);
				await unitOfWork.DebtRequestRepository.UpdateCreditorStatusAsync(debtRequestId, CreditorRequestStatus.Paid);
				await unitOfWork.SaveAsync();
				await unitOfWork.CommitTransactionAsync();
				return modifieds;
			}
			catch(Exception ex)
			{
				logger.LogError(ex, "Error confirming pending debt request transactions for debtRequestId {DebtRequestId}", debtRequestId);
				await unitOfWork.RollbackAsync();
				throw;
			}
		}

		private async Task<UserDebtRequestVm> UpdateCreditorResetStatusAsync(int debtRequestId, Guid userId, CreditorRequestStatus newStatus)
		{
			await unitOfWork.StartTransactionAsync();
			try
			{
				await unitOfWork.DebtRequestRepository.RemoveAllAppTransactionsFromDebtRequestAsync(debtRequestId, userId);
				var debtRequest = await unitOfWork.DebtRequestRepository.UpdateCreditorStatusAsync(debtRequestId, newStatus);
				await unitOfWork.SaveAsync();
				await unitOfWork.CommitTransactionAsync();
				return debtRequest;
			}
			catch
			{
				await unitOfWork.RollbackAsync();
				throw;
			}
		}

		public async Task<UserDebtRequestVm> UpdateDebtorStatusAsync(int debtRequestId, DebtorRequestStatus status, Guid userId, DateTime dateTime)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync<UserDebtRequestVm>(debtRequestId, userId) 
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			if (debtRequest.TrxCount == 0)
			{
				debtRequest = await unitOfWork.DebtRequestRepository.UpdateDebtorStatusAsync(debtRequestId, status);
				await unitOfWork.SaveAsync();
				return debtRequest;
			}

			if(status == DebtorRequestStatus.Pending || status == DebtorRequestStatus.Rejected)
			{
				return await UpdateDebtorResetStatusAsync(debtRequestId, userId, status);
			}
			else if (status == DebtorRequestStatus.Paid)
			{
				var modifieds = await ConfirmPendingDebtorDebtRequestTrxsAsync(debtRequestId, userId, dateTime);
				var modifiedDebtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync<TrxModifiedDebtRequestVm>(debtRequestId, userId)
                    ?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
				modifiedDebtRequest.ModifiedTrxs = modifieds;
                return modifiedDebtRequest;
			}
			else
			{
				throw new ServiceException("Cannot change debtor status when there are associated transactions. Only reset is allowed.");
			}
		}

		private async Task<UserDebtRequestVm> UpdateDebtorResetStatusAsync(int debtRequestId, Guid userId, DebtorRequestStatus newStatus)
		{
			await unitOfWork.StartTransactionAsync();
			try
			{
				await unitOfWork.DebtRequestRepository.RemoveAllAppTransactionsFromDebtRequestAsync(debtRequestId, userId);
				var debtRequest = await unitOfWork.DebtRequestRepository.UpdateDebtorStatusAsync(debtRequestId, newStatus);
				await unitOfWork.SaveAsync();
				await unitOfWork.CommitTransactionAsync();
				return debtRequest;
			}
			catch
			{
				await unitOfWork.RollbackAsync();
				throw;
			}
		}

		private async Task<IEnumerable<SpendItemModified>> ConfirmPendingDebtorDebtRequestTrxsAsync(int debtRequestId, Guid userId, DateTime dateTime)
		{
			var debtRequest = await unitOfWork.DebtRequestRepository.GetDebtRequestsByIdAsync(debtRequestId, userId, true) as UserDebtRequestVm
				?? throw new ServiceException($"Debt request with id {debtRequestId} not found");
			if (debtRequest.UserTrxs == null || !debtRequest.UserTrxs.Any())
			{
				throw new ServiceException($"No transactions found for debt request {debtRequestId}");
			}
			var trxIds = debtRequest.UserTrxs.Select(x => x.SpendId).ToList();
			await unitOfWork.StartTransactionAsync();
			try
			{
				var modifieds = await appTransactionsSubService.ExecuteConfirmPendingTransactionsAsync(trxIds, dateTime);
				debtRequest = await unitOfWork.DebtRequestRepository.UpdateDebtorStatusAsync(debtRequestId, DebtorRequestStatus.Paid);
				await unitOfWork.SaveAsync();
				await unitOfWork.CommitTransactionAsync();
				return modifieds;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error confirming pending debt request transactions for debtRequestId {DebtRequestId}", debtRequestId);
				await unitOfWork.RollbackAsync();
				throw;
			}
		}

		public async Task<CreateSimpleDebtRequestVm> GetCreateSimpleDebtRequestVmAsync(Guid userId)
		{
			var currencies = await unitOfWork.CurrenciesRepository.GetCurrenciesAsync();
			var users = await unitOfWork.UserRepository.GetAppUsersAsync();
			var usersVm = users.Where(x => x.UserId != userId && x.HasRole(RoleId.User)).Select(x =>
			{
				return new BasicUserViewModel(x.UserId, x.Username, x.Name);
			}).ToList();

			return new CreateSimpleDebtRequestVm(currencies, usersVm);
		}

		public async Task<UserDebtRequestVm> CreateSimpleDebtRequestAsync(ClientDebtRequest clientDebtRequest)
		{
			return await unitOfWork.DebtRequestRepository.CreateSimpleDebtRequestAsync(clientDebtRequest);
		}

		public async Task DeleteDebtRequestAsync(int debtRequestId)
		{
			await unitOfWork.DebtRequestRepository.DeleteDebtRequestAsync(debtRequestId);
			await unitOfWork.SaveAsync();
		}

		public async Task<IReadOnlyCollection<UserDebtRequestVm>> GetDebtRequestByUserIdAsync(Guid userId)
		{
			return await unitOfWork.DebtRequestRepository.GetDebtRequestsByUserAsync(userId);
		}

		private static IReadOnlyCollection<NewAppTransactionByAccount> CreateNewAppTransactions(
			string userId,
			DebtRequestVm debtRequestVm,
			TransactionTypeIds transactionTypeId,
			IReadOnlyCollection<NewDebtRequestAppTrx> newDebtRequestAppTrxes)
		{
			var newAppTransactions = new List<NewAppTransactionByAccount>();
			foreach (var newDebtRequestAppTrx in newDebtRequestAppTrxes)
			{
				var accountId = newDebtRequestAppTrx.AccountId;
				if (accountId == 0)
				{
					throw new ServiceException("AccountId is required for each transaction");
				}
				var newAppTransaction = new NewAppTransactionByAccount
				(
					userId,
					newDebtRequestAppTrx.Amount,
					debtRequestVm.EventDate,
					newDebtRequestAppTrx.TrxTypeId,
					debtRequestVm.Currency.Id,
					newDebtRequestAppTrx.Description,
					true,
					accountId,
					transactionTypeId
				);
				newAppTransactions.Add(newAppTransaction);
			}
			return newAppTransactions;
		}
	}
}
