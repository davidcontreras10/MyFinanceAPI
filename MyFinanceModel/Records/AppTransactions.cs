using MyFinanceModel.ClientViewModel;
using System;

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

}
