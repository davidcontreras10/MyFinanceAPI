using Microsoft.AspNetCore.Http;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceWebApiCore.Models;
using MyFinanceWebApiCore.Services.FinancialEntityFiles;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public class ExcelFileReaderService(IFinancialEntitiesRepository financialEntitiesRepository, IFormFileExcelReader formFileExcelReader, IScotiabankFileReader scotiabankFileReader) : IExcelFileReaderService
	{
		private readonly IFinancialEntitiesRepository _financialEntitiesRepository = financialEntitiesRepository;
		private readonly IFormFileExcelReader _formFileExcelReader = formFileExcelReader;
		private readonly IScotiabankFileReader _scotiabankFileReader = scotiabankFileReader;

		public async Task<IReadOnlyCollection<FileBankTransaction>> ReadTransactionsFromFile(IFormFile file, string financialEntityName)
		{
			var finacialEntity = await _financialEntitiesRepository.GetByMatchedName(financialEntityName);
			if (finacialEntity == null)
			{
				throw new ArgumentException($"Financial entity {financialEntityName} does not exist");
			}

			switch (financialEntityName)
			{
				case FinancialEntityNames.Scotiabank:
					return await ReadScotiabankFileAsync(file, financialEntityName);
				default:
					throw new NotSupportedException($"Financial Entity {financialEntityName} not supported");
			}

		}

		private async Task<IReadOnlyCollection<FileBankTransaction>> ReadScotiabankFileAsync(IFormFile file, string financialEntityName)
		{
			var fileData = await _formFileExcelReader.ReadExcelFileAsync(file, 0, financialEntityName);
			return _scotiabankFileReader.ReadValues(fileData);
		}


	}
}
