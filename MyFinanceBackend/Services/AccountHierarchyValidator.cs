using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
	/// <summary>
	/// Enforces the account tree rules: an account has at most one parent ("main account"), and the tree
	/// is two levels deep (a main account can't itself be a sub-account).
	/// The relationship is stored as AccountInclude rows on the child (AccountId = child, AccountIncludeId = parent).
	/// </summary>
	public static class AccountHierarchyValidator
	{
		public static void ValidateParents(
			int? accountId,
			IReadOnlyCollection<int> requestedParentIds,
			ISet<int> ownedAccountIds,
			ISet<int> accountIdsWithParent,
			int accountSubAccountsCount)
		{
			if (requestedParentIds == null || requestedParentIds.Count == 0)
			{
				return;
			}

			if (requestedParentIds.Count > 1)
			{
				throw BadRequest("An account can have only one main account.");
			}

			var parentId = requestedParentIds.Single();
			if (accountId.HasValue && parentId == accountId.Value)
			{
				throw BadRequest("An account can't be a sub-account of itself.");
			}

			if (!ownedAccountIds.Contains(parentId))
			{
				throw BadRequest("The main account does not exist.");
			}

			if (accountIdsWithParent.Contains(parentId))
			{
				throw BadRequest("The main account is already a sub-account. Sub-accounts can't have their own sub-accounts.");
			}

			if (accountSubAccountsCount > 0)
			{
				throw BadRequest($"This account has {accountSubAccountsCount} sub-account(s), so it can't become a sub-account itself.");
			}
		}

		private static ServiceException BadRequest(string message)
		{
			return new ServiceException(message, HttpStatusCode.BadRequest);
		}
	}
}
