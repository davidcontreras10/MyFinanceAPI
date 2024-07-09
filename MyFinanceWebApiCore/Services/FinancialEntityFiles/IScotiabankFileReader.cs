using MyFinanceModel.ClientViewModel;
using System.Collections.Generic;

namespace MyFinanceWebApiCore.Services.FinancialEntityFiles
{
	public interface IScotiabankFileReader
	{
		IReadOnlyCollection<FileBankTransaction> ReadValues(object[,] values);
	}
}
