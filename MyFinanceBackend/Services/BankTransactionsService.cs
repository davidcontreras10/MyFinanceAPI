using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
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

		public async Task<IReadOnlyCollection<BankTrxReqResp>> GetFileBankTransactionState(IReadOnlyCollection<FileBankTransaction> fileBankTransactions, FinancialEntityFile financialEntityFile)
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

			return results;
		}
	}
}
