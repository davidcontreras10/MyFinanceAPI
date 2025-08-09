using MyFinanceModel.BankTrxCategorization;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IBankTrxCategorizationRepository
	{
		Task<IReadOnlyCollection<OutGptClassifiedExpense>> ClassifyExpensesWithGptAsync(
			List<ExpenseToClassify> inputExpenses,
			List<Gpt.Category> categories,
			List<Gpt.Account> accountDescriptions,
			List<InHisotricClassfiedExpense> historicalExamples
		);
	}
}
