using Microsoft.AspNetCore.Http;
using MyFinanceModel;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public class EPPlusFormFileExcelReader : IFormFileExcelReader
	{
		public EPPlusFormFileExcelReader()
		{
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
		}

		public async Task<object[,]> ReadExcelFileAsync(IFormFile file, int worksheetIndex, string fileName)
		{
			var excelPackage = await ReadFileAsync(file);
			if (excelPackage == null || excelPackage.Workbook.Worksheets.Count < worksheetIndex + 1)
			{
				throw new FinancialEntityFileUploadException("No worksheets", fileName);
			}

			var worksheet = excelPackage.Workbook.Worksheets[worksheetIndex];
			var rowCount = worksheet.Dimension.Rows;
			var colCount = worksheet.Dimension.Columns;
			if(rowCount == 0 || colCount == 0)
			{
				throw FinancialEntityFileUploadException.Empty(fileName);
			}

			var fileData = new object[rowCount, colCount];
			for (var row = 1; row <= rowCount; row++)
			{
				for (int col = 1; col <= colCount; col++)
				{
					fileData[row - 1, col - 1] = worksheet.Cells[row, col].Value;
				}
			}

			return fileData;
		}


		private static async Task<ExcelPackage> ReadFileAsync(IFormFile file)
		{
			using var stream = new MemoryStream();
			await file.CopyToAsync(stream);
			return new ExcelPackage(stream);
		}
	}
}
