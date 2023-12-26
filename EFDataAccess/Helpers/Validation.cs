using System;
using System.Collections.Generic;
using System.Text;

namespace EFDataAccess.Helpers
{
	internal static class Validation
	{
		public static bool IsNotNullOrDefault<T>(Nullable<T> value) where T : struct
		{
			var defT = default(T);
			return value != null && !value.Value.Equals(defT);
		} 
	}
}
