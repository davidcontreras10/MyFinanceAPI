using EFDataAccess.Models;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using System.Linq;
using AppUser = EFDataAccess.Models.AppUser;

namespace EFDataAccess.Helpers
{
	public static class ToModel
	{
		public static DebtRequestVm ToDebtRequestVm(this EFDebtRequest debtRequest)
		{
			return new DebtRequestVm
			{
				Id = debtRequest.Id,
				EventName = debtRequest.EventName,
				EventDescription = debtRequest.EventDescription,
				EventDate = debtRequest.EventDate,
				Amount = debtRequest.Amount,
				Currency = debtRequest.Currency?.ToCurrencyViewModel(),
				Creditor = debtRequest.CreditorUser?.ToAppUser(),
				Debtor = debtRequest.DebtorUser?.ToAppUser()
			};
		}

		public static BasicCurrencyViewModel ToBasicCurrencyViewModel(this Currency currency)
		{

			return new BasicCurrencyViewModel
			{
				CurrencyId = currency.CurrencyId,
				CurrencyName = currency.Name,
				Symbol = currency.Symbol,
				IsoCode = currency.IsoCode
			};
		}

		public static MyFinanceModel.AppUser ToAppUser(this AppUser appUser)
		{
			return new MyFinanceModel.AppUser
			{
				Name = appUser.Name,
				PrimaryEmail = appUser.PrimaryEmail,
				UserId = appUser.UserId,
				Username = appUser.Username,
				Roles = appUser.UserRoles?.Select(x => x.ToAppRole()).ToList()
			};
		}

		public static AppRole ToAppRole(this EFAppRole appRole)
		{
			return new AppRole
			{
				Id = appRole.Id,
				Name = appRole.Name,
				Level = appRole.Level
			};
		}
	}
}
