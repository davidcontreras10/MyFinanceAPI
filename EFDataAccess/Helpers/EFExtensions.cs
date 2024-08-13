using EFDataAccess.Models;
using MyFinanceModel.ViewModel;

namespace EFDataAccess.Helpers
{
	internal static class EFExtensions
	{
		public static CurrencyViewModel ToCurrencyViewModel(this Currency currency)
		{
			return new CurrencyViewModel
			{
				CurrencyId = currency.CurrencyId,
				CurrencyName = currency.Name,
				IsoCode = currency.IsoCode,
				Symbol = currency.Symbol
			};
		}

		public static MethodId ToMethodId(this CurrencyConverterMethod currencyConverterMethod, bool isSelected = false, bool isDefault = false)
		{
			return new MethodId
			{
				Id = currencyConverterMethod.CurrencyConverterMethodId,
				Name = currencyConverterMethod.Name,
				IsSelected = isSelected,
				IsDefault = isDefault
			};
		}

		public static SpendTypeViewModel ToSpendTypeViewModel(this SpendType spendType, int? defaultSpendTypeId)
		{
			return new SpendTypeViewModel
			{
				Description = spendType.Description,
				IsDefault = defaultSpendTypeId != null && defaultSpendTypeId.Value == spendType.SpendTypeId,
				SpendTypeId = spendType.SpendTypeId,
				SpendTypeName = spendType.Name
			};
		}
	}
}
