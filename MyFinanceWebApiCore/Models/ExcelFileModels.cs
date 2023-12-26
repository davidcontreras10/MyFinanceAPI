using OfficeOpenXml.Style;
using OfficeOpenXml;
using System.Drawing;
using System;

namespace MyFinanceWebApiCore.Models
{
	public class ExcelCellStyle
	{
		public int? FontSize { get; set; }
		public Color? BackgroundColor { get; set; }
		public Color? FontColor { get; set; }
		public bool? FontBold { get; set; }
	}

	public abstract class BaseExcelCell
	{
		protected object Value { get; }

		public ExcelCellStyle CellStyle { get; set; }

		protected BaseExcelCell(object value)
		{
			Value = value;
		}

		public void GenerateExcelCell(ExcelRange excelRange)
		{
			excelRange.Value = Value;
			if (CellStyle?.BackgroundColor != null)
			{
				excelRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
				excelRange.Style.Fill.BackgroundColor.SetColor(CellStyle.BackgroundColor.Value);
			}

			if (CellStyle?.FontSize != null)
			{
				excelRange.Style.Font.Size = CellStyle.FontSize.Value;
			}

			if (CellStyle?.FontColor != null)
			{
				excelRange.Style.Font.Color.SetColor(CellStyle.FontColor.Value);
			}

			if (CellStyle?.FontBold != null)
			{
				excelRange.Style.Font.Bold = CellStyle.FontBold.Value;
			}

			GenerateCustomExcelCell(excelRange);
		}

		protected abstract void GenerateCustomExcelCell(ExcelRange excelRange);
	}

	public class ExcelBasicCell : BaseExcelCell
	{
		public ExcelBasicCell(object value) : base(value) { }

		protected override void GenerateCustomExcelCell(ExcelRange excelRange)
		{
		}
	}

	public class ExcelCurrencyNumber : BaseExcelCell
	{
		private readonly string _currencySymbol;
		private string Format => $"{_currencySymbol}#,##0.00";

		public ExcelCurrencyNumber(string currencySymbol, float value) : base(value)
		{
			_currencySymbol = currencySymbol;
		}

		protected override void GenerateCustomExcelCell(ExcelRange excelRange)
		{
			excelRange.Style.Numberformat.Format = Format;
		}
	}

	public class DateTimeExcelCell : BaseExcelCell
	{
		public DateTimeExcelCell(DateTime value) : base(value)
		{
		}

		protected override void GenerateCustomExcelCell(ExcelRange excelRange)
		{
			excelRange.Style.Numberformat.Format = "yyyy-mm-dd";
		}
	}
}
