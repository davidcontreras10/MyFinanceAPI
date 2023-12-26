using MyFinanceModel.ClientViewModel;
using System;
using System.Linq;

namespace EFDataAccess.Extensions
{
	public static class ClientEditAccountExtensions
	{
		public static bool Contains(this ClientEditAccount clientEditAccount, AccountFiedlds field)
		{
			return clientEditAccount.EditAccountFields.Contains(field);
		}
	}
}
