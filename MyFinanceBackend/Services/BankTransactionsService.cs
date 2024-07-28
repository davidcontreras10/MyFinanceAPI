using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel.BankTransactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public class BankTransactionsService(IUnitOfWork unitOfWork) : IBankTransactionsService
	{
		private readonly IUnitOfWork _unitOfWork = unitOfWork;

		public async Task<IReadOnlyCollection<BankTrxReqResp>> ProcessAndGetFileBankTransactionState(IReadOnlyCollection<FileBankTransaction> fileBankTransactions, FinancialEntityFile financialEntityFile)
		{
			if (fileBankTransactions == null || fileBankTransactions.Count == 0)
			{
				return Array.Empty<BankTrxReqResp>();
			}

			var financialEntity = await _unitOfWork.FinancialEntitiesRepository.GetByFinancialEntityFile(financialEntityFile);
			var dbBankTrxs = await _unitOfWork.BankTransactionsRepository
				.GetBasicBankTransactionByIdsAsync(fileBankTransactions.Select(x => x.TransactionId), financialEntity.FinancialEntityId);
			var results = new List<BankTrxReqResp>();
			foreach (var transaction in fileBankTransactions)
			{
				var bankTrxReqResp = new BankTrxReqResp
				{
					Transaction = transaction
				};

				var dbTrx = dbBankTrxs.FirstOrDefault(db => db.BankTransactionId == transaction.TransactionId);
				bankTrxReqResp.DbStatus = dbTrx == null ? BankTransactionStatus.NotExisting : dbTrx.Status;
				results.Add(bankTrxReqResp);
			}

			var notExistingRecords = results.Where(r => r.DbStatus == BankTransactionStatus.NotExisting);
			var currencyCodes = notExistingRecords.Select(r => r.Transaction.CurrencyCode).Distinct().ToList();
			var currencies = await _unitOfWork.CurrenciesRepository.GetCurrenciesByCodesAsync(currencyCodes);
			var inserts = notExistingRecords.Select(r => new BasicBankTransaction
			{
				BankTransactionId = r.Transaction.TransactionId,
				CurrencyId = currencies.First(x => x.IsoCode == r.Transaction.CurrencyCode).Id,
				FinancialEntityId = financialEntity.FinancialEntityId,
				OriginalAmount = r.Transaction.OriginalAmount,
				Status = BankTransactionStatus.Inserted,
				TransactionDate = r.Transaction.TransactionDate
			});
			await _unitOfWork.BankTransactionsRepository.AddBasicBankTransactionAsync(inserts);
			await _unitOfWork.SaveAsync();
            return results;
		}
	}
}
