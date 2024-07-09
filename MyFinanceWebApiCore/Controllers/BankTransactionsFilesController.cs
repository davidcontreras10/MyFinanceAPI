using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceWebApiCore.Models;
using MyFinanceWebApiCore.Services;
using System.IO;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BankTransactionsFilesController(IExcelFileReaderService excelFileReaderService) : BaseApiController
	{
		private readonly IExcelFileReaderService _excelFileReaderService = excelFileReaderService;

		[HttpPost("upload")]
		public async Task<IActionResult> UploadExcelFile(IFormFile file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file uploaded.");

			var fileExtension = Path.GetExtension(file.FileName);

			if (fileExtension != ".xls" && fileExtension != ".xlsx")
				return BadRequest("Invalid file type. Only .xls and .xlsx are allowed.");

			var transactions = await _excelFileReaderService.ReadTransactionsFromFile(file, FinancialEntityNames.Scotiabank);
			return Ok(transactions);
		}
	}
}
