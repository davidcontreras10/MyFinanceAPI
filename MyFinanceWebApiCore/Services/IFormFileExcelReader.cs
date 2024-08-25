using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public interface IFormFileExcelReader
	{
		Task<object[,]> ReadExcelFileAsync(IFormFile file, int worksheetIndex, string fileName);
	}
}
