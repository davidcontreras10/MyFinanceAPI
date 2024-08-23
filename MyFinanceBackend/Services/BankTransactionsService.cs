using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel.BankTransactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class BankTransactionsService(IUnitOfWork unitOfWork, IAppTransactionsSubService appTransactionsSubService) : IBankTransactionsService
	{
		public async Task DeleteBankTransactionAsync(BankTrxId bankTrxId)
		{
			var ids = new[] { bankTrxId };
			var deletedTrxIds = await unitOfWork.BankTransactionsRepository.ClearTrxsFromBankTrxsAsync(ids);
			var trxIds = deletedTrxIds.Select(x => x.SpendId).ToList();
			var modifieds = (trxIds.Count > 0) ? await unitOfWork.SpendsRepository.DeleteTransactionsAsync(trxIds) : Array.Empty<SpendItemModified>();
			await unitOfWork.BankTransactionsRepository.UpdateBankTrxStatusAsync(ids, BankTransactionStatus.Inserted);
			await unitOfWork.SaveAsync();
		}

		public async Task<UserProcessingResponse> ResetBankTransactionAsync(BankTrxId bankTrxId)
		{
			var ids = new[] { bankTrxId };
			var deletedTrxIds = await unitOfWork.BankTransactionsRepository.ClearTrxsFromBankTrxsAsync(ids);
			var trxIds = deletedTrxIds.Select(x => x.SpendId).ToList();
			var modifieds = (trxIds.Count > 0) ? await unitOfWork.SpendsRepository.DeleteTransactionsAsync(trxIds) : Array.Empty<SpendItemModified>();
			await unitOfWork.BankTransactionsRepository.UpdateBankTrxStatusAsync(ids, BankTransactionStatus.Inserted);
			await unitOfWork.SaveAsync();
			var dbTrxs = await unitOfWork.BankTransactionsRepository.GetBankTransactionDtoByIdsAsync(ids);
			return new UserProcessingResponse
			{
				BankTransactions = dbTrxs.Select(ToBankTrxItemReqResp).ToList(),
				ItemModifieds = modifieds.ToList()
			};
		}

		public async Task<BankTrxReqResp> GetBankTransactionBySearchCriteriaAsync(IUserSearchCriteria userSearchCriteria)
		{
			if(userSearchCriteria == null) return new BankTrxReqResp();
			var bankTrxs = await unitOfWork.BankTransactionsRepository.GetBankTransactionsBySearchCriteriaAsync(userSearchCriteria);
			var bankTransactions = await unitOfWork.BankTransactionsRepository.GetBankTransactionDtoByIdsAsync(bankTrxs.Select(x => x.BankTrxId));
			var currencies = await unitOfWork.CurrenciesRepository.GetCurrenciesByCodesAsync(bankTransactions.Select(x => x.Currency.IsoCode));
			var accountsPerCurrencies = await unitOfWork.AccountRepository.GetAccountsByCurrenciesAsync(currencies.Select(c => c.Id), userSearchCriteria.RequesterUserId);
			return new BankTrxReqResp
			{
				BankTransactions = bankTransactions.Select(ToBankTrxItemReqResp).ToList(),
				AccountsPerCurrencies = accountsPerCurrencies
			};
		}

		public async Task<UserProcessingResponse> ProcessUserBankTrxAsync(string userId, IReadOnlyCollection<BankItemRequest> bankItemRequests)
		{
			await unitOfWork.StartTransactionAsync();
			try
			{
				if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId), "User Id is required");
				if (bankItemRequests == null || bankItemRequests.Count == 0) throw new ArgumentNullException(nameof(bankItemRequests), "Bank Item Requests are required");
				var bankItemRequestsList = bankItemRequests.ToList();
				await UpdateBankTransactionsDescriptionsAsync(bankItemRequestsList);
				var ignoreTrxs = bankItemRequests.Where(trx => trx.RequestIgnore).ToList();
				var modifieds = (await IgnoreTransactionsAsync(ignoreTrxs.Select(trx => trx.BankTrxId).ToList())).ToList();
				bankItemRequestsList = bankItemRequestsList.Where(trx => !ignoreTrxs.Contains(trx)).ToList();
				var dbBankTrxs = await unitOfWork.BankTransactionsRepository.GetBasicBankTransactionByIdsAsync(bankItemRequestsList.Select(x => x.BankTrxId));
				var processedDbTrxs = dbBankTrxs.Where(x => x.Status == BankTransactionStatus.Processed);
				bankItemRequestsList = bankItemRequestsList
					.Where(x => !processedDbTrxs.Any(db => db.BankTransactionId == x.BankTrxId.TransactionId && db.FinancialEntityId == x.BankTrxId.FinancialEntityId)).ToList();
				var singleTrxModifieds = (await ProcessBankTransactionsWithAppTrxsAsync(bankItemRequestsList, userId)).ToList();
				modifieds.AddRange(singleTrxModifieds);
				var dbTrxs = await unitOfWork.BankTransactionsRepository.GetBankTransactionDtoByIdsAsync(bankItemRequests.Select(x => x.BankTrxId));
				await unitOfWork.CommitTransactionAsync();
				return new UserProcessingResponse
				{
					BankTransactions = dbTrxs.Select(ToBankTrxItemReqResp).ToList(),
					ItemModifieds = modifieds
				};
			}
			catch (Exception ex)
			{
				await unitOfWork.RollbackAsync();
				throw new InvalidOperationException("Error processing bank transactions", ex);
			}
		}

		public async Task<BankTrxReqResp> InsertAndGetFileBankTransactionState(
			IReadOnlyCollection<FileBankTransaction> fileBankTransactions,
			FinancialEntityFile financialEntityFile,
			string userId
			)
		{
			if (fileBankTransactions == null || fileBankTransactions.Count == 0)
			{
				return new BankTrxReqResp();
			}

			BankTrxReqResp response = new();
			var financialEntity = await unitOfWork.FinancialEntitiesRepository.GetByFinancialEntityFile(financialEntityFile);
			var currencyCodes = fileBankTransactions.Select(r => r.CurrencyCode).Distinct().ToList();
			var currencies = await unitOfWork.CurrenciesRepository.GetCurrenciesByCodesAsync(currencyCodes);
			var dbBankTrxs = await unitOfWork.BankTransactionsRepository
				.GetBasicBankTransactionByIdsAsync(fileBankTransactions.Select(x => x.TransactionId), financialEntity.FinancialEntityId);
			var results = new List<BankTrxItemReqResp>();
			foreach (var transaction in fileBankTransactions.Where(trx => !string.IsNullOrWhiteSpace(trx.TransactionId)))
			{
				var bankTrxReqResp = new BankTrxItemReqResp
				{
					FileTransaction = transaction,
					FinancialEntityId = financialEntity.FinancialEntityId,
					Currency = currencies.First(x => x.IsoCode == transaction.CurrencyCode),
				};

				var dbTrx = dbBankTrxs.FirstOrDefault(db => db.BankTransactionId == transaction.TransactionId);
				if (dbTrx != null)
				{
					bankTrxReqResp.DbStatus = dbTrx.Status;
					bankTrxReqResp.FileTransaction.Description = dbTrx.Description;
					if (dbTrx.Transactions is not null)
					{
						bankTrxReqResp.ProcessData = new BankTrxItemReqResp.DbData
						{
							Transactions = dbTrx.Transactions
						};
					}
				}
				else
				{
					bankTrxReqResp.DbStatus = BankTransactionStatus.NotExisting;
				}
				results.Add(bankTrxReqResp);
			}

			var notExistingRecords = results.Where(r => r.DbStatus == BankTransactionStatus.NotExisting);
			var inserts = notExistingRecords.Select(r => new BasicBankTransactionDto
			{
				BankTransactionId = r.FileTransaction.TransactionId,
				CurrencyId = currencies.First(x => x.IsoCode == r.FileTransaction.CurrencyCode).Id,
				FinancialEntityId = financialEntity.FinancialEntityId,
				OriginalAmount = r.FileTransaction.OriginalAmount,
				Status = BankTransactionStatus.Inserted,
				TransactionDate = r.FileTransaction.TransactionDate
			});
			await unitOfWork.BankTransactionsRepository.AddBasicBankTransactionAsync(inserts);
			await unitOfWork.SaveAsync();
			response.BankTransactions = results;
			response.AccountsPerCurrencies = await unitOfWork.AccountRepository.GetAccountsByCurrenciesAsync(currencies.Select(c => c.Id), userId);
			return response;
		}

		private async Task<IEnumerable<SpendItemModified>> ProcessMultipleTransactionsAsync(IReadOnlyCollection<BankItemRequest> bankItemRequests, string userId)
		{
			if (bankItemRequests == null || bankItemRequests.Count == 0) return [];
			var multipleTrxs = bankItemRequests.Where(x => x.IsMultipleTrx == true).ToList();
			if (multipleTrxs.Count == 0) return [];
			var dbBankTrxs = await unitOfWork.BankTransactionsRepository.GetBasicBankTransactionByIdsAsync(bankItemRequests.Select(x => x.BankTrxId));
			var allowedStatus = new[] { BankTransactionStatus.Ignored, BankTransactionStatus.Inserted };
			if (dbBankTrxs.Any(x => !allowedStatus.Contains(x.Status)))
			{
				throw new InvalidOperationException("The transactions are not in the correct status");
			}

			var newBankTrxs = new List<NewMultipleTrxBankTransaction>();
			var modifiedtems = new List<SpendItemModified>();
			foreach (var bankTrx in bankItemRequests)
			{
				var dbTrx = dbBankTrxs.FirstOrDefault(x => x.Id == bankTrx.BankTrxId)
					?? throw new InvalidOperationException("The transaction is not in the database");
			}

			throw new NotImplementedException();
		}

		private async Task<IEnumerable<SpendItemModified>> ProcessBankTransactionsWithAppTrxsAsync(IReadOnlyCollection<BankItemRequest> bankItemRequests, string userId)
		{
			if (bankItemRequests == null || bankItemRequests.Count == 0) return [];
			var dbBankTrxs = await unitOfWork.BankTransactionsRepository.GetBasicBankTransactionByIdsAsync(bankItemRequests.Select(x => x.BankTrxId));
			var allowedStatus = new[] { BankTransactionStatus.Ignored, BankTransactionStatus.Inserted };
			if (dbBankTrxs.Any(x => !allowedStatus.Contains(x.Status)))
			{
				throw new InvalidOperationException("The transactions are not in the correct status");
			}

			var requestableTrxs = bankItemRequests.Select(br => new IdentifiableRequest<BankItemRequest>(br, Guid.NewGuid())).ToList();
			var appTrxRequests = new List<NewAppTransactionByAccount>();
			foreach (var requestableTrx in requestableTrxs)
			{
				var bankTrx = requestableTrx.Request;
				var dbTrx = dbBankTrxs.FirstOrDefault(x => x.Id == bankTrx.BankTrxId)
					?? throw new InvalidOperationException("The transaction is not in the database");
				if (bankTrx.IsMultipleTrx == true)
				{
					var newAppTransactionsByAccount = CreateMultipleNewAppTransactionByAccount(bankTrx, dbTrx, userId, requestableTrx.RequestId);
					var originalAmount = dbTrx.OriginalAmount ?? throw new InvalidOperationException("The original amount is not set");
					if (newAppTransactionsByAccount.Sum(x => x.Amount) != (float)originalAmount) throw new InvalidOperationException("The sum of the transactions is not equal to the original amount");
					appTrxRequests.AddRange(newAppTransactionsByAccount);
				}
				else
				{
					var newAppTransactionByAccount = CreateSingleNewAppTransactionByAccount(bankTrx, dbTrx, userId, requestableTrx.RequestId);
					appTrxRequests.Add(newAppTransactionByAccount);
				}
			}

			var modifiedtems = await appTransactionsSubService.AddMultipleTrxsByAccountAsync(appTrxRequests);
			var newBankTrxs = new List<NewTrxBankTransaction>();
			foreach (var requestableTrx in requestableTrxs)
			{
				var trxModifieds = GetTrxItemModifiedRecords(requestableTrx, modifiedtems);
				if (!trxModifieds.Any()) throw new InvalidOperationException("The transaction was not created");
				var spendOnPeriods = trxModifieds.Select(x => new SpendOnPeriodId(x.SpendId, x.AccountPeriodId));
				NewTrxBankTransaction newTrxBankTransaction = new(requestableTrx.Request.BankTrxId, requestableTrx.Request.Description, spendOnPeriods);
				newBankTrxs.Add(newTrxBankTransaction);
			}

			await unitOfWork.BankTransactionsRepository.NewSingleTrxBankTransactionsAsync(newBankTrxs);
			await unitOfWork.SaveAsync();
			return modifiedtems.Select(SpendItemModified.To);
		}

		private static IEnumerable<TrxItemModifiedRecord> GetTrxItemModifiedRecords(IdentifiableRequest<BankItemRequest> request, IEnumerable<TrxItemModifiedRecord> trxItemModifiedRecords)
		{
			if (request.Request.IsMultipleTrx == true)
			{
				var accountIds = request.Request.Transactions.Select(x => x.AccountId).ToList();
				return trxItemModifiedRecords.Where(x => x.RequestId == request.RequestId && accountIds.Contains(x.AccountId));
			}

			return trxItemModifiedRecords.Where(x => x.RequestId == request.RequestId && x.AccountId == request.Request.AccountId);
		}

		private static IReadOnlyCollection<NewAppTransactionByAccount> CreateMultipleNewAppTransactionByAccount(BankItemRequest bankTrx, BasicBankTransactionDto dbTrx, string userId, Guid requestId)
		{
			if (bankTrx.RequestIgnore) throw new InvalidOperationException("The transaction is ignored");
			if (bankTrx.Transactions == null || bankTrx.Transactions.Count == 0) throw new InvalidOperationException("The transactions are not set");
			var date = dbTrx.TransactionDate ?? throw new InvalidOperationException("The transaction date is not set");
			var appTransactions = new List<NewAppTransactionByAccount>();
			var currency = dbTrx.CurrencyId ?? throw new InvalidOperationException("The currency is not set");
			foreach (var transaction in bankTrx.Transactions)
			{
				var spendTypeId = transaction.SpendTypeId > 0 ? transaction.SpendTypeId : throw new InvalidOperationException("The spend type is not set");
				NewAppTransactionByAccount newAppTransactionByAccount =
					new(userId, (float)transaction.Amount, date, spendTypeId, currency, transaction.Description, transaction.IsPending, transaction.AccountId, TransactionTypeIds.Spend, requestId);
				appTransactions.Add(newAppTransactionByAccount);

			}
			return appTransactions;
		}

		private static NewAppTransactionByAccount CreateSingleNewAppTransactionByAccount(BankItemRequest bankTrx, BasicBankTransactionDto dbTrx, string userId, Guid requestId)
		{
			var date = dbTrx.TransactionDate ?? throw new InvalidOperationException("The transaction date is not set");
			var spendTypeId = bankTrx.SpendTypeId ?? throw new InvalidOperationException("The spend type is not set");
			var currency = dbTrx.CurrencyId ?? throw new InvalidOperationException("The currency is not set");
			var isPending = bankTrx.IsPending ?? throw new InvalidOperationException("Is pending is not set");
			var accountId = bankTrx.AccountId ?? throw new InvalidOperationException("The account id is not set");
			NewAppTransactionByAccount newAppTransactionByAccount =
				new(userId, (float)dbTrx.OriginalAmount, date, spendTypeId, currency, bankTrx.Description, isPending, accountId, TransactionTypeIds.Spend, requestId);
			return newAppTransactionByAccount;
		}

		private static BankTrxItemReqResp ToBankTrxItemReqResp(BankTransactionDto basicBankTransaction)
		{
			var fileTransaction = new FileBankTransaction
			{
				TransactionId = basicBankTransaction.BankTrxId.TransactionId,
				CurrencyCode = basicBankTransaction.Currency.IsoCode,
				OriginalAmount = basicBankTransaction.OriginalAmount,
				TransactionDate = basicBankTransaction.TransactionDate,
				Description = basicBankTransaction.Description
			};

			return new BankTrxItemReqResp
			{
				FinancialEntityId = basicBankTransaction.BankTrxId.FinancialEntityId,
				FileTransaction = fileTransaction,
				Currency = basicBankTransaction.Currency,
				DbStatus = basicBankTransaction.Status,
				ProcessData = new BankTrxItemReqResp.DbData
				{
					Transactions = basicBankTransaction.Transactions
				}
			};
		}

		private async Task UpdateBankTransactionsDescriptionsAsync(IReadOnlyCollection<BankItemRequest> bankItemRequests)
		{
			if (bankItemRequests == null || bankItemRequests.Count == 0) return;
			var bankTrxDescriptions = new List<BankTrxDescription>();
			foreach (var trx in bankItemRequests)
			{
				bankTrxDescriptions.Add(new BankTrxDescription
				(
					trx.Description,
					trx.BankTrxId
				));
			}

			await unitOfWork.BankTransactionsRepository.UpdateBankTransactionsDescriptionsAsync(bankTrxDescriptions);
			await unitOfWork.SaveAsync();
		}

		private async Task<IReadOnlyCollection<SpendItemModified>> IgnoreTransactionsAsync(IReadOnlyCollection<BankTrxId> bankTrxIds)
		{
			if (bankTrxIds == null || bankTrxIds.Count == 0) return Array.Empty<SpendItemModified>();
			var deletedTrxIds = await unitOfWork.BankTransactionsRepository.ClearTrxsFromBankTrxsAsync(bankTrxIds);
			await unitOfWork.BankTransactionsRepository.UpdateBankTrxStatusAsync(bankTrxIds, BankTransactionStatus.Ignored);
			var trxIds = deletedTrxIds.Select(x => x.SpendId).ToList();
			var result = (trxIds.Count > 0) ? await unitOfWork.SpendsRepository.DeleteTransactionsAsync(trxIds) : Array.Empty<SpendItemModified>(); ;
			await unitOfWork.SaveAsync();
			return result.ToList();
		}
	}
}
