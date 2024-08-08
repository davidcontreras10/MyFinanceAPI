using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel.BankTransactions;
using MyFinanceWebApiCore.Services;
using System.IO;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BankTransactionsFilesController(IExcelFileReaderService excelFileReaderService, IBankTransactionsService bankTransactionsService) : BaseApiController
	{
		private readonly IExcelFileReaderService _excelFileReaderService = excelFileReaderService;
		private readonly IBankTransactionsService _bankTransactionsService = bankTransactionsService;

		[HttpPost("Upload")]
		public async Task<IActionResult> UploadExcelFile(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			var fileExtension = Path.GetExtension(file.FileName);

			if (fileExtension != ".xls" && fileExtension != ".xlsx")
			{
				return BadRequest("Invalid file type. Only .xls and .xlsx are allowed.");
			}

			var transactions = await _excelFileReaderService.ReadTransactionsFromFile(file, FinancialEntityFile.Scotiabank);
			return Ok(transactions);
		}

		[HttpPost("UploadRequest")]
		public async Task<ActionResult<BankTrxReqResp>> GetFileBankTransactionState(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			var fileExtension = Path.GetExtension(file.FileName);

			if (fileExtension != ".xls" && fileExtension != ".xlsx")
			{
				return BadRequest("Invalid file type. Only .xls and .xlsx are allowed.");
			}

			var transactions = await _excelFileReaderService.ReadTransactionsFromFile(file, FinancialEntityFile.Scotiabank);
			var userId = GetUserId();
			var resultTrxs = await _bankTransactionsService.ProcessAndGetFileBankTransactionState(transactions, FinancialEntityFile.Scotiabank, userId);
			return Ok(resultTrxs);
		}
	}
}
