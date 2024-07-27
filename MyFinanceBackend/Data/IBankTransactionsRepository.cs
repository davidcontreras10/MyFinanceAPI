using MyFinanceModel.Dto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IBankTransactionsRepository
	{
		Task<IReadOnlyCollection<BasicBankTransaction>> GetBasicBankTransactionByIdsAsync(IEnumerable<string> ids, int financialEntityId);
	}
}
