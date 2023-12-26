using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess.Helpers
{
	public static class StringHelper
	{
		public static Guid ToGuid(this string value)
		{
			return new Guid(value);
		}
	}
}
