using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MyFinanceModel.Utilities
{
	public static class EnumUtil
	{
		public static IEnumerable<T> GetValues<T>()
		{
			return Enum.GetValues(typeof(T)).Cast<T>();
		}
	}

	public static class CustomParse
    {
        public static bool TryParseEnum<TEnum>(this int enumValue, out TEnum retVal)
        {
            retVal = default(TEnum);
            var success = Enum.IsDefined(typeof(TEnum), enumValue);
            if (success)
            {
                retVal = (TEnum)Enum.ToObject(typeof(TEnum), enumValue);
            }
            return success;
        }
    }

    public static class CustomSerializationInfoExtensions
    {
        public static bool ExistsKey(this SerializationInfo info, string key, bool ignoreCase = false)
        {
            foreach (var serializationEntry in info)
            {
                var comparison = ignoreCase
                    ? StringComparison.InvariantCultureIgnoreCase
                    : StringComparison.InvariantCulture;
                if (string.Compare(serializationEntry.Name, key, comparison) == 0)
                {
                    return true;
                }
            }

            return false; 
        }

        public static SerializationEntry? GetSerializationEntry(this SerializationInfo info, string key,
            bool ignoreCase = false)
        {
            foreach (var serializationEntry in info)
            {
                var comparison = ignoreCase
                    ? StringComparison.InvariantCultureIgnoreCase
                    : StringComparison.InvariantCulture;
                if (string.Compare(serializationEntry.Name, key, comparison) == 0)
                {
                    return serializationEntry;
                }
            }

            return null;
        }
    }
}
