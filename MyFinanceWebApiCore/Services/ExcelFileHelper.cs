using DContre.MyFinance.StUtilities;
using MyFinanceModel.ViewModel;
using MyFinanceWebApiCore.Models;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace MyFinanceWebApiCore.Services
{
	public class ExcelFileHelper
	{
		public static string GetFileName(IEnumerable<AccountFinanceViewModel> accounts)
		{
			if (accounts == null || !accounts.Any())
			{
				return string.Empty;
			}

			var concatenatedNames = accounts.Aggregate(string.Empty, (current, account) => current + (account.AccountName + "_"));
			concatenatedNames += DateTime.Now.ToString("u");
			concatenatedNames = concatenatedNames.Truncate(35) + ".xlsx";
			return concatenatedNames;
		}

		public static byte[] GenerateFile(IReadOnlyCollection<AccountFinanceViewModel> accounts)
		{
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			using (var package = new ExcelPackage())
			{
				foreach (var accountFinanceViewModel in accounts)
				{
					var sheet = package.Workbook.Worksheets.Add(accountFinanceViewModel.AccountName);
					WriteAccountWorksheet(sheet, accountFinanceViewModel);
				}

				return package.GetAsByteArray();
			}
		}

		private static void WriteAccountWorksheet(
			ExcelWorksheet excelWorksheet,
			AccountFinanceViewModel accountFinanceViewModel
		)
		{
			try
			{
				WriteTitle(excelWorksheet, accountFinanceViewModel);
				const int startColPosition = 1;
				const int spendHeadersRow = 2;
				WriteSpendsHeader(excelWorksheet, spendHeadersRow, startColPosition);
				const int startRowPosition = spendHeadersRow + 1;
				var spends = accountFinanceViewModel.SpendViewModels.OrderBy(x => x.SpendDate).ToList();
				WriteSpendsList(excelWorksheet, spends, startRowPosition, startColPosition,
					accountFinanceViewModel.CurrencyId, accountFinanceViewModel.CurrencySymbol);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw new Exception("Error trying to draw the file", e);
			}
		}

		private static void WriteSpendsHeader(ExcelWorksheet excelWorksheet, int headerRow, int startCol)
		{
			var headers = GetSpendHeaders();
			foreach (var baseExcelCell in headers)
			{
				baseExcelCell.GenerateExcelCell(excelWorksheet.Cells[headerRow, startCol++]);
			}
		}

		private static void WriteTitle(ExcelWorksheet excelWorksheet, AccountFinanceViewModel accountFinanceViewModel)
		{
			var title = $"{accountFinanceViewModel.AccountName} -- {accountFinanceViewModel.AccountPeriodName}";
			var titleCell = new ExcelBasicCell(title)
			{
				CellStyle = new ExcelCellStyle
				{
					FontSize = 15,
					FontBold = true
				}
			};
			titleCell.GenerateExcelCell(excelWorksheet.Cells[1, 1]);
			excelWorksheet.Cells[1, 8].Merge = true;
		}

		private static void WriteSpendsList(
			ExcelWorksheet worksheet,
			IReadOnlyCollection<SpendViewModel> spendViewModels,
			int startRow,
			int startCol,
			int accountCurrencyId,
			string accountCurrencySymbol
		)
		{
			var rowPos = startRow;
			var colPos = startCol;
			var maxCols = 0;
			foreach (var spendViewModel in spendViewModels)
			{
				var baseCells = GetSpendViewModelLine(spendViewModel, accountCurrencySymbol);
				if (baseCells.Length > maxCols)
				{
					maxCols = baseCells.Length;
				}
				foreach (var baseExcelCell in baseCells)
				{
					baseExcelCell.GenerateExcelCell(worksheet.Cells[rowPos, colPos]);
					colPos++;
				}

				rowPos++;
				colPos = startCol;
			}

			var startHeader = startRow - 1;
			worksheet.Cells[startHeader, startCol, spendViewModels.Count + startHeader, maxCols].AutoFitColumns();
		}

		private static BaseExcelCell[] GetSpendHeaders()
		{
			var headerStyle = new ExcelCellStyle
			{
				BackgroundColor = Color.FromArgb(227, 227, 227)
			};
			return new BaseExcelCell[]
			{
				new ExcelBasicCell("Date")
				{
					CellStyle = headerStyle
				},
				new ExcelBasicCell("Original Amount")
				{
					CellStyle = headerStyle
				},
				new ExcelBasicCell("Account Amount")
				{
					CellStyle = headerStyle
				},
				new ExcelBasicCell("Type")
				{
					CellStyle = headerStyle
				},
				new ExcelBasicCell("Description")
				{
					CellStyle = headerStyle
				}
			};
		}

		private static BaseExcelCell[] GetSpendViewModelLine(
			SpendViewModel spendViewModel,
			string accountCurrencySymbol
		)
		{
			var isSaving = spendViewModel.AmountTypeId == 2;
			var multiplier = isSaving ? 1 : -1;
			var fontColor = isSaving ? Color.Green : Color.Red;
			var background = spendViewModel.IsPending ? (Color?)Color.LavenderBlush : null;
			var originalAmount = new ExcelCurrencyNumber(spendViewModel.CurrencySymbol, spendViewModel.OriginalAmount * multiplier)
			{
				CellStyle = new ExcelCellStyle
				{
					FontColor = fontColor,
					BackgroundColor = background
				}
			};
			var accountAmount = new ExcelCurrencyNumber(accountCurrencySymbol, spendViewModel.ConvertedAmount * multiplier)
			{
				CellStyle = new ExcelCellStyle
				{
					FontColor = fontColor,
					BackgroundColor = background
				}
			};
			var date = new DateTimeExcelCell(spendViewModel.SpendDate);
			return new BaseExcelCell[]
			{
				date,
				originalAmount,
				accountAmount,
				new ExcelBasicCell(spendViewModel.SpendTypeName),
				new ExcelBasicCell(spendViewModel.Description)
			};
		}
	}
}
