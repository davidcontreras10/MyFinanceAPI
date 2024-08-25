using Microsoft.AspNetCore.Http;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public class ExcelFileReaderService(IFormFileExcelReader formFileExcelReader, Func<FinancialEntityFile, IFinancialEntityFileReader> financialEntityFileReaderFactory) : IExcelFileReaderService
	{
		private readonly IFormFileExcelReader _formFileExcelReader = formFileExcelReader;
		private readonly Func<FinancialEntityFile, IFinancialEntityFileReader> _financialEntityFileReaderFactory = financialEntityFileReaderFactory;

		public async Task<IReadOnlyCollection<FileBankTransaction>> ReadTransactionsFromFile(IFormFile file, FinancialEntityFile financialEntity)
		{

			var fileData = await _formFileExcelReader.ReadExcelFileAsync(file, 0, financialEntity.ToString());
			var fileReader = _financialEntityFileReaderFactory(financialEntity);
			return fileReader.ReadValues(fileData);
		}
	}
}
