﻿using MyFinanceModel.Dto;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel.BankTransactions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IBankTransactionsRepository
	{
		Task DeleteAsync(BankTrxId bankTrxId);
		Task<IReadOnlyCollection<BankTrxAppTrx>> GetBankTransactionsBySearchCriteriaAsync(IUserSearchCriteria userSearchCriteria);
		Task<IReadOnlyCollection<BankTransactionDto>> GetBankTransactionDtoByIdsAsync(IEnumerable<BankTrxId> bankTrxIds);
		Task NewSingleTrxBankTransactionsAsync(IEnumerable<NewTrxBankTransaction> newTrxBankTransactions);
		Task UpdateBankTransactionsDescriptionsAsync(IEnumerable<BankTrxDescription> bankTrxDescriptions);
		Task<IReadOnlyCollection<BasicBankTransactionDto>> GetBasicBankTransactionByIdsAsync(IEnumerable<BankTrxId> bankTrxIds);
		Task<IReadOnlyCollection<SpendOnPeriodId>> ClearTrxsFromBankTrxsAsync(IReadOnlyCollection<BankTrxId> bankTrxIds);
		Task<IReadOnlyCollection<BasicBankTransactionDto>> GetBasicBankTransactionByIdsAsync(IEnumerable<string> ids, int financialEntityId);
		Task AddBasicBankTransactionAsync(IEnumerable<BasicBankTransactionDto> basicBankTransactions);
		Task UpdateBankTrxStatusAsync(IReadOnlyCollection<BankTrxId> bankTrxIds, BankTransactionStatus newStatus);
		Task UpdateBankTransactionsDatesAsync(IEnumerable<BankTrxDate> trxDates);
	}
}
