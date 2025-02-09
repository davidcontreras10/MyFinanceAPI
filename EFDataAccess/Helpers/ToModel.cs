using EFDataAccess.Models;
using MyFinanceModel;
using MyFinanceModel.Enums;
using MyFinanceModel.ViewModel;
using System;
using System.Linq;
using AppUser = EFDataAccess.Models.AppUser;

namespace EFDataAccess.Helpers
{
	public static class ToModel
	{
		public static T ToDebtRequestVm<T>(this EFDebtRequest debtRequest, Guid? reqUserId = null) where T : DebtRequestVm
		{
			T debtRequestVm;
			if (typeof(T) == typeof(UserDebtRequestVm) && reqUserId != null)
			{
				debtRequestVm = new UserDebtRequestVm(reqUserId.Value) as T;
			}
			else
			{
				debtRequestVm = new DebtRequestVm() as T;
			}

			debtRequestVm.Id = debtRequest.Id;
			debtRequestVm.EventName = debtRequest.EventName;
			debtRequestVm.EventDescription = debtRequest.EventDescription;
			debtRequestVm.EventDate = debtRequest.EventDate;
			debtRequestVm.Amount = debtRequest.Amount;
			debtRequestVm.Currency = debtRequest.Currency?.ToCurrencyViewModel();
			debtRequestVm.Creditor = debtRequest.CreditorUser?.ToAppUser<Creditor>(creditorRequestStatus: debtRequest.CreditorStatus);
			debtRequestVm.Debtor = debtRequest.DebtorUser?.ToAppUser<Debtor>(debtorRequestStatus: debtRequest.DebtorStatus);
			debtRequestVm.CreatedDate = debtRequest.CreatedDate;
			return debtRequestVm;
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

		public static T ToAppUser<T>(this AppUser appUser, 
			DebtorRequestStatus debtorRequestStatus = DebtorRequestStatus.Undefined,
			CreditorRequestStatus creditorRequestStatus = CreditorRequestStatus.Undefined
			) where T : MyFinanceModel.AppUser, new()
		{
			var model = new T
			{
				Name = appUser.Name,
				PrimaryEmail = appUser.PrimaryEmail,
				UserId = appUser.UserId,
				Username = appUser.Username,
				Roles = appUser.UserRoles?.Select(x => x.ToAppRole()).ToList()
			};

			if(model is Debtor debtor)
			{
				debtor.Status = debtorRequestStatus;
			}
			else if (model is Creditor creditor)
			{
				creditor.Status = creditorRequestStatus;
			}
			return model;
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
