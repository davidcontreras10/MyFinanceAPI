using System.Net;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
	/// <summary>What the repository knows about an account type, for validating a choice.</summary>
	public class AccountTypeUsage
	{
		public bool Exists { get; set; }
		public bool IsActive { get; set; }

		/// <summary>The account being edited already has this type (always false when creating).</summary>
		public bool AccountHasIt { get; set; }
	}

	/// <summary>
	/// An inactive account type can't be chosen for new accounts or when changing an account's type,
	/// but an account that already has it keeps it.
	/// </summary>
	public static class AccountTypeRules
	{
		public static void ValidateSelectable(AccountTypeUsage usage)
		{
			if (!usage.Exists)
			{
				throw BadRequest("The account type does not exist.");
			}

			if (!usage.IsActive && !usage.AccountHasIt)
			{
				throw BadRequest("This account type is no longer available.");
			}
		}

		private static ServiceException BadRequest(string message)
		{
			return new ServiceException(message, HttpStatusCode.BadRequest);
		}
	}
}
