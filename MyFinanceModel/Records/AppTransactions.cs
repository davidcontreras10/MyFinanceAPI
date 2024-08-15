using MyFinanceModel.ClientViewModel;
using System;
using System.Collections.Generic;

namespace MyFinanceModel.Records
{
	public record class NewAppTransactionByAccount(
		string UserId, 
		float Amount, 
		DateTime SpendDate, 
		int SpendTypeId, 
		int CurrencyId, 
		string Description, 
		bool IsPending, 
		int AccountId, 
		TransactionTypeIds TransactionType
	);
	
	public record class SpendCurrencyConvertibleItem(Guid Guid, ISpendCurrencyConvertible SpendCurrencyConvertible);

	public record class SpendCurrencyConvertibleResult(Guid Guid, IReadOnlyCollection<AddSpendAccountDbValues> DbValues);
}
