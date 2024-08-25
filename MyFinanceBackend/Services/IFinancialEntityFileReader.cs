using MyFinanceModel.ClientViewModel;
using System.Collections.Generic;

namespace MyFinanceBackend.Services
{
	public interface IFinancialEntityFileReader
	{
		IReadOnlyCollection<FileBankTransaction> ReadValues(object[,] values);
	}
}
