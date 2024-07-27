using MyFinanceModel.Enums;
using System;

namespace MyFinanceWebApiCore.Models
{
	public static class FinancialEntityNames
	{
		public const string BacSanJose = "Bac San Jose";
		public const string Promerica = "Promerica";
		public const string Scotiabank = "Scotiabank";

		public static string GetNameByEnum(FinancialEntityFile financialEntityFile)
		{
			return financialEntityFile switch
			{
				FinancialEntityFile.None => throw new Exception("Invalid FinancialEntityFile"),
				FinancialEntityFile.Scotiabank => Scotiabank,
				FinancialEntityFile.Promerica => Promerica,
				FinancialEntityFile.BacSanJose => BacSanJose,
				_ => throw new Exception("Invalid FinancialEntityFile"),
			};
		}
	}
}
