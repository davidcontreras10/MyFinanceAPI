using EFDataAccess.Models;
using MyFinanceModel.ViewModel;

namespace EFDataAccess.Helpers
{
	internal static class EFExtensions
	{
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
