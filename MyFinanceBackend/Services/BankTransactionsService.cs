using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel.BankTransactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class BankTransactionsService(IUnitOfWork unitOfWork) : IBankTransactionsService
	{
		private readonly IUnitOfWork _unitOfWork = unitOfWork;

		public async Task<BankTrxReqResp> ProcessAndGetFileBankTransactionState(
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
			var financialEntity = await _unitOfWork.FinancialEntitiesRepository.GetByFinancialEntityFile(financialEntityFile);
			var currencyCodes = fileBankTransactions.Select(r => r.CurrencyCode).Distinct().ToList();
			var currencies = await _unitOfWork.CurrenciesRepository.GetCurrenciesByCodesAsync(currencyCodes);
			var dbBankTrxs = await _unitOfWork.BankTransactionsRepository
				.GetBasicBankTransactionByIdsAsync(fileBankTransactions.Select(x => x.TransactionId), financialEntity.FinancialEntityId);
			var results = new List<BankTrxItemReqResp>();
			foreach (var transaction in fileBankTransactions)
			{
				var bankTrxReqResp = new BankTrxItemReqResp
				{
					FileTransaction = transaction,
					Currency = currencies.First(x => x.IsoCode == transaction.CurrencyCode),
				};

				var dbTrx = dbBankTrxs.FirstOrDefault(db => db.BankTransactionId == transaction.TransactionId);
				if (dbTrx != null)
				{
					bankTrxReqResp.DbStatus = dbTrx.Status;
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
			await _unitOfWork.BankTransactionsRepository.AddBasicBankTransactionAsync(inserts);
			await _unitOfWork.SaveAsync();
			response.BankTransactions = results;
			response.AccountsPerCurrencies = await _unitOfWork.AccountRepository.GetAccountsByCurrenciesAsync(currencies.Select(c => c.Id), userId);
			return response;
		}
	}
}
