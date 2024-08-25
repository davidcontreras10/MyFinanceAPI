using Microsoft.AspNetCore.Http;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public interface IExcelFileReaderService
	{
		Task<IReadOnlyCollection<FileBankTransaction>> ReadTransactionsFromFile(IFormFile file, FinancialEntityFile financialEntity);
	}
}
