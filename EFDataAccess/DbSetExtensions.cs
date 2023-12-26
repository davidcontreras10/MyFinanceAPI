using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess
{
	internal static class DbSetExtensions
	{
		public static void RemoveWhere<T>(this DbSet<T> dbSet, Func<T, bool> predicate) where T : class
		{
			var removeItems = dbSet.Where(predicate);
			dbSet.RemoveRange(removeItems);
		}

		public async static Task RemoveWhereAsync<T>(this DbSet<T> dbSet, Func<T, bool> predicate) where T : class
		{
			var removeItems = dbSet.RemoveWhereAsync(predicate);
			await dbSet.RemoveWhereAsync(predicate);
		}
	}
}
