using EFDataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFDataAccess.Repositories
{
	/// <summary>
	/// A user's account groups always have a compact 1..N sequence of positions. All work happens on the
	/// in-memory entities, so it doesn't depend on what the database currently holds for unsaved changes.
	/// </summary>
	public static class AccountGroupOrdering
	{
		/// <summary>
		/// Puts <paramref name="target"/> at <paramref name="requestedPosition"/> (1-based; clamped to the valid
		/// range, so anything past the end means "last") and renumbers every group 1..N. The target can be new,
		/// i.e. not part of <paramref name="groups"/> yet.
		/// </summary>
		public static void Place(IEnumerable<AccountGroup> groups, AccountGroup target, int requestedPosition)
		{
			var ordered = Sort(groups.Where(group => !ReferenceEquals(group, target)));
			var index = Math.Clamp(requestedPosition, 1, ordered.Count + 1) - 1;
			ordered.Insert(index, target);
			Number(ordered);
		}

		/// <summary>Closes gaps and ties, keeping the current order.</summary>
		public static void Renumber(IEnumerable<AccountGroup> groups)
		{
			Number(Sort(groups));
		}

		private static List<AccountGroup> Sort(IEnumerable<AccountGroup> groups)
		{
			return groups
				.OrderBy(group => group.AccountGroupPosition ?? int.MaxValue)
				.ThenBy(group => group.AccountGroupId)
				.ToList();
		}

		private static void Number(List<AccountGroup> ordered)
		{
			for (var i = 0; i < ordered.Count; i++)
			{
				ordered[i].AccountGroupPosition = i + 1;
			}
		}
	}
}
