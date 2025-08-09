using MyFinanceModel.BankTrxCategorization;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Services
{
	public interface IExpensesClassificationSubService
	{
		Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExistingBankTransactionsAsync(IReadOnlyCollection<string> refNumbers, int financialEntityId, string userId);
		Task<IReadOnlyCollection<ClassifiedBankTrx>> GetClassifiedBankTransactionsAsync(string userId);
	}
}