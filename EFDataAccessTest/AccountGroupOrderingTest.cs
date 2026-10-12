using System.Collections.Generic;
using System.Linq;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using NUnit.Framework;

namespace EFDataAccessTest
{
	public class AccountGroupOrderingTest
	{
		private static List<AccountGroup> Groups(params int[] ids)
		{
			// ids in the order they are stored: position = index + 1
			return ids.Select((id, i) => new AccountGroup { AccountGroupId = id, AccountGroupPosition = i + 1 }).ToList();
		}

		private static AccountGroup Find(List<AccountGroup> groups, int id)
		{
			return groups.Single(group => group.AccountGroupId == id);
		}

		private static int[] OrderOf(IEnumerable<AccountGroup> groups)
		{
			return groups.OrderBy(group => group.AccountGroupPosition).Select(group => group.AccountGroupId).ToArray();
		}

		private static void AssertCompactPositions(IEnumerable<AccountGroup> groups)
		{
			var positions = groups.Select(group => group.AccountGroupPosition).OrderBy(position => position).ToArray();
			Assert.That(positions, Is.EqualTo(Enumerable.Range(1, positions.Length).Cast<int?>().ToArray()));
		}

		[Test]
		public void MovingUpPutsTheGroupAtTheRequestedPosition()
		{
			var groups = Groups(1, 2, 3, 4);

			AccountGroupOrdering.Place(groups, Find(groups, 4), 2);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 1, 4, 2, 3 }));
			Assert.That(Find(groups, 4).AccountGroupPosition, Is.EqualTo(2));
			AssertCompactPositions(groups);
		}

		[Test]
		public void MovingDownPutsTheGroupAtTheRequestedPosition()
		{
			var groups = Groups(1, 2, 3, 4, 5);

			AccountGroupOrdering.Place(groups, Find(groups, 2), 4);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 1, 3, 4, 2, 5 }));
			Assert.That(Find(groups, 2).AccountGroupPosition, Is.EqualTo(4));
			AssertCompactPositions(groups);
		}

		[Test]
		public void MovingToTheLastPosition()
		{
			var groups = Groups(1, 2, 3, 4);

			AccountGroupOrdering.Place(groups, Find(groups, 1), 4);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 2, 3, 4, 1 }));
		}

		[Test]
		public void PositionsPastTheEndMeanLast()
		{
			var groups = Groups(1, 2, 3);

			AccountGroupOrdering.Place(groups, Find(groups, 1), 99);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 2, 3, 1 }));
			AssertCompactPositions(groups);
		}

		[TestCase(0)]
		[TestCase(-5)]
		public void PositionsBelowOneMeanFirst(int requested)
		{
			var groups = Groups(1, 2, 3);

			AccountGroupOrdering.Place(groups, Find(groups, 3), requested);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 3, 1, 2 }));
		}

		[Test]
		public void PlacingAtItsCurrentPositionChangesNothing()
		{
			var groups = Groups(1, 2, 3, 4);

			AccountGroupOrdering.Place(groups, Find(groups, 3), 3);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 1, 2, 3, 4 }));
		}

		[Test]
		public void NewGroupIsInsertedAtTheRequestedPosition()
		{
			var groups = Groups(1, 2, 3);
			var created = new AccountGroup { AccountGroupId = 10 };

			AccountGroupOrdering.Place(groups, created, 2);

			var all = groups.Concat(new[] { created }).ToList();
			Assert.That(OrderOf(all), Is.EqualTo(new[] { 1, 10, 2, 3 }));
			AssertCompactPositions(all);
		}

		[Test]
		public void NewGroupAtTheCountTakesThatPlaceBeforeTheLastOne()
		{
			var groups = Groups(1, 2, 3);
			var created = new AccountGroup { AccountGroupId = 10 };

			AccountGroupOrdering.Place(groups, created, 3);

			Assert.That(OrderOf(groups.Concat(new[] { created })), Is.EqualTo(new[] { 1, 2, 10, 3 }));
		}

		[Test]
		public void NewGroupAfterTheLastPositionIsAppended()
		{
			var groups = Groups(1, 2, 3);
			var created = new AccountGroup { AccountGroupId = 10 };

			AccountGroupOrdering.Place(groups, created, 4);

			Assert.That(OrderOf(groups.Concat(new[] { created })), Is.EqualTo(new[] { 1, 2, 3, 10 }));
		}

		[Test]
		public void FirstGroupOfAUserGetsPositionOne()
		{
			var created = new AccountGroup { AccountGroupId = 10 };

			AccountGroupOrdering.Place(new List<AccountGroup>(), created, 7);

			Assert.That(created.AccountGroupPosition, Is.EqualTo(1));
		}

		[Test]
		public void RenumberClosesGapsAndKeepsTheOrder()
		{
			var groups = new List<AccountGroup>
			{
				new AccountGroup { AccountGroupId = 1, AccountGroupPosition = 1 },
				new AccountGroup { AccountGroupId = 2, AccountGroupPosition = 2 },
				new AccountGroup { AccountGroupId = 3, AccountGroupPosition = 5 },
				new AccountGroup { AccountGroupId = 4, AccountGroupPosition = 9 },
			};

			AccountGroupOrdering.Renumber(groups);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 1, 2, 3, 4 }));
			AssertCompactPositions(groups);
		}

		[Test]
		public void RenumberBreaksTiesById()
		{
			var groups = new List<AccountGroup>
			{
				new AccountGroup { AccountGroupId = 8, AccountGroupPosition = 2 },
				new AccountGroup { AccountGroupId = 3, AccountGroupPosition = 2 },
				new AccountGroup { AccountGroupId = 5, AccountGroupPosition = 1 },
			};

			AccountGroupOrdering.Renumber(groups);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 5, 3, 8 }));
		}

		[Test]
		public void GroupsWithoutAPositionGoLast()
		{
			var groups = new List<AccountGroup>
			{
				new AccountGroup { AccountGroupId = 1, AccountGroupPosition = null },
				new AccountGroup { AccountGroupId = 2, AccountGroupPosition = 1 },
			};

			AccountGroupOrdering.Renumber(groups);

			Assert.That(OrderOf(groups), Is.EqualTo(new[] { 2, 1 }));
		}
	}
}
