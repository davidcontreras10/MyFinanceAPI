using Microsoft.EntityFrameworkCore.Storage;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MyFinanceWebApiCore.Services.FinancialEntityFiles
{
	public class ScotiabankFileReader : IFinancialEntityFileReader
	{
		private const string BankName = "Scotiabank";
		private const string DateFormat = "dd/MM/yyyy";

		private static readonly string[] HeaderColumns =
		[
			"Número de Referencia",//0
			"Fecha de Movimiento",//1
			"Descripción",//2
			"Monto",//3
			"Moneda",//4
			"Tipo"//5
		];

		private const string AcceptedTipoValue = "DEBITO";
		private static readonly string[] AcceptedMonedaValues = 
		[
			"CRC",
			"USD"
		];

		public IReadOnlyCollection<FileBankTransaction> ReadValues(object[,] values)
		{
			if (values == null || values.Length == 0)
			{
				return Array.Empty<FileBankTransaction>();
			}

			var transactions = new List<FileBankTransaction>();
			ValidateHeaders(values);
			for (var i = 1; i < values.GetLength(0); i++)
			{
				if(IsValidRow(values, i))
				{
					transactions.Add(new FileBankTransaction
					{
						CurrencyCode = values[i, 4]?.ToString(),
						Description = values[i, 2]?.ToString(),
						OriginalAmount = NumUtils.ToDecimal(values[i, 3]),
						TransactionDate = GetDate(values[i, 1]),
						TransactionId = values[i, 0]?.ToString()
					});
				}
			}

			return transactions;
		}

		private static DateTime? GetDate(object value)
		{
			if (value == null) return null;
			return DateTime.TryParseExact(value.ToString(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var converted) ? converted : null;
		}

		private static bool IsValidRow(object[,] values, int i)
		{
			var currencyValue = values[i,4]?.ToString();
			return values[i, 5]?.ToString() == AcceptedTipoValue && AcceptedMonedaValues.Contains(currencyValue);
		}

		private static void ValidateHeaders(object[,] values)
		{
			var index = 0;
			const int headersIndex = 0;
			if (HeaderColumns.Length != values.GetLength(1))
			{
				throw new FinancialEntityFileUploadException($"Unexpected row length", BankName);
			}
			foreach (var column in HeaderColumns)
			{
				if (values[headersIndex, index++]?.ToString() != column)
				{
					throw new FinancialEntityFileUploadException($"Column {column} expected but not found", BankName);
				}
			}
		}

		private record HeaderColumn(string Name, string Index);
	}
}
