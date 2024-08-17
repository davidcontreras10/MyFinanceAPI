using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using MyFinanceModel.Records;
using MyFinanceModel.ViewModel.BankTransactions;
using MyFinanceWebApiCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			var resultTrxs = await _bankTransactionsService.InsertAndGetFileBankTransactionState(transactions, FinancialEntityFile.Scotiabank, userId);
			return Ok(resultTrxs);
		}

		[HttpPost("files/scotiabank")]
		public async Task<ActionResult<BankTrxReqResp>> GetScotiabankTransactionState(IFormFile file)
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
			var resultTrxs = await _bankTransactionsService.InsertAndGetFileBankTransactionState(transactions, FinancialEntityFile.Scotiabank, userId);
			return Ok(resultTrxs);
		}

		[HttpGet("app-transactions")]
		public async Task<ActionResult<BankTrxReqResp>> GetBankTransactionByAppTrxId([FromQuery] int[] transactionId)
		{
			var userId = GetUserId();
			return await _bankTransactionsService.GetBankTransactionByAppTrxIdAsync(transactionId, userId);
		}

		[HttpDelete("{transactionId}/{financialEntityId}")]
		public async Task<ActionResult> DeleteBankTransaction([FromRoute] string transactionId, [FromRoute] int financialEntityId)
		{
			await _bankTransactionsService.DeleteBankTransactionAsync(new BankTrxId(financialEntityId, transactionId));
			return Ok();
		}

		[HttpPut("{transactionId}/{financialEntityId}")]
		public async Task<ActionResult<UserProcessingResponse>> ResetBankTransaction([FromRoute] string transactionId, [FromRoute] int financialEntityId)
		{
			var result = await _bankTransactionsService.ResetBankTransactionAsync(new BankTrxId(financialEntityId, transactionId));
			return Ok(result);
		}

		[HttpPost("ProcessRequest")]
		public async Task<ActionResult<IReadOnlyCollection<BankTrxItemReqResp>>> ProcessUserRequests(IReadOnlyCollection<ClientBankItemRequest> clientBankItemRequests)
		{
			var userId = GetUserId();
			var bankTransactions = clientBankItemRequests.Select(item => new BankItemRequest(
				new BankTrxId(item.FinancialEntityId, item.TransactionId),
				item.RequestIgnore,
				item.Description,
				item.IsMultipleTrx,
				item.AccountId,
				item.SpendTypeId,
				item.IsPending,
				item.Transactions
			)).ToList();

			var resultTrxs = await _bankTransactionsService.ProcessUserBankTrxAsync(userId, bankTransactions);
			return Ok(resultTrxs);
		}
	}
}
