using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using MyFinanceModel;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Services
{
	public class EDRFormFileExcelReader : IFormFileExcelReader
	{
		public Task<object[,]> ReadExcelFileAsync(IFormFile file, int worksheetIndex, string fileName)
		{
			var dataSet = ReadExcelFile(file);
			if (dataSet == null || dataSet.Tables.Count < worksheetIndex + 1)
			{
				throw new FinancialEntityFileUploadException("No worksheets", fileName);
			}

			var table = dataSet.Tables[worksheetIndex];
			var rowCount = table.Rows.Count;
			var colCount = table.Columns.Count;
			if (rowCount == 0 || colCount == 0)
			{
				throw FinancialEntityFileUploadException.Empty(fileName);
			}
			var fileData = new object[rowCount + 1, colCount];
			var colIndex = 0;
			foreach (var column in table.Columns)
			{
				fileData[0, colIndex++] = column?.ToString();
			}

			for (var rowIndex = 1; rowIndex < rowCount; rowIndex++)
			{
				var row = table.Rows[rowIndex];
				for(var j = 0; j < colCount; j++)
				{
					fileData[rowIndex, j] = row[j];
				}
			}

			return Task.FromResult(fileData);
		}

		private static DataSet ReadExcelFile(IFormFile file)
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			using var stream = new MemoryStream();
			file.CopyTo(stream);
			stream.Position = 0;
			using var reader = ExcelReaderFactory.CreateReader(stream);
			var result = reader.AsDataSet(new ExcelDataSetConfiguration
			{
				ConfigureDataTable = _ => new ExcelDataTableConfiguration
				{
					UseHeaderRow = true
				}
			});

			return result;
		}
	}
}
