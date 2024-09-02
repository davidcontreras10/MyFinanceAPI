using System.Collections.Generic;
using System.Linq;
using MyFinanceModel;

namespace MyFinanceBackend.Services
{
	internal static class ServicesUtils
	{
		public static IEnumerable<T> SmartConcat<T>(params IEnumerable<T>[] arrayList) where T : ItemModified, new()
		{
			var concatedlist = new List<T>();
			foreach(var list in arrayList)
			{
				foreach(var item in list)
				{
					if (concatedlist.All(item2 => !item.Equals(item2)))
					{
						concatedlist.Add(item);
					}
				}
			}

			return concatedlist;
		}
	}
}
