using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;

namespace MyFinanceModel.Records
{
	public record class BankTrxId(int FinancialEntityId, string TransactionId);

	public record class BankItemRequest(
		BankTrxId BankTrxId,
		bool RequestIgnore,
		string Description,
		bool? IsMultipleTrx,
		int? AccountId,
		int? SpendTypeId,
		bool? IsPending,
		IReadOnlyCollection<ClientBankTrxRequest> Transactions);


	public record class BankTrxDescription(string Description, BankTrxId BankTrxId);

	public record class NewSingleTrxBankTransaction(BankTrxId BankTrxId, string Description, SpendOnPeriodId SpendOnPeriodId);

	public record class NewMultipleTrxBankTransaction(BankTrxId BankTrxId, string Description, IEnumerable<SpendOnPeriodId> SpendOnPeriodIds);

	public record class NewTrxBankTransaction(BankTrxId BankTrxId, string Description, IEnumerable<SpendOnPeriodId> SpendOnPeriodIds);

	public record class BankTransactionDto(
		BankTrxId BankTrxId,
		string Description,
		BankTransactionStatus Status,
		CurrencyViewModel Currency,
		decimal OriginalAmount,
		DateTime? TransactionDate,
		IReadOnlyCollection<SpendViewModel> Transactions
	);

	public record class BankTrxAppTrx(BankTrxId BankTrxId, int appTrxId);
}
