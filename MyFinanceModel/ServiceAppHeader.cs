using MyFinanceModel.Utilities;
using System;
using System.Collections.Generic;

namespace MyFinanceModel
{
	public class ServiceAppHeader
	{
		private ServiceAppHeader() { }

		public enum ServiceAppHeaderType
		{
			Unknown = 0,
			Restrict = 1,
			ApplicationModule = 2
		}

		public string Name { get; set; }
		public string Description { get; set; }
		public bool IsRequired { get; set; }
		public ServiceAppHeaderType Type { get; set; }
		public string ValueType { get; set; } = "string";
		public IList<object> Enums { get; set; }

		public static ServiceAppHeader GetServiceAppHeader(ServiceAppHeaderType type)
		{
			if (type == ServiceAppHeaderType.ApplicationModule)
			{
				return new ServiceAppHeader()
				{
					Description = "Module name",
					Name = "$module",
					Type = ServiceAppHeaderType.ApplicationModule,
					ValueType = "string",
					Enums = Types<ApplicationModules>(),
					IsRequired = true
				};
			}

			if (type == ServiceAppHeaderType.Restrict)
			{
				return new ServiceAppHeader()
				{
					Description = "Restrict response object",
					Name = "$restrict",
					Type = ServiceAppHeaderType.Restrict,
					ValueType = "string"
				};
			}

			throw new ArgumentException(nameof(type));
		}

		private static IList<object> Types<T>()
		{
			var enums = EnumUtil.GetValues<T>();
			var objects = new List<object>();
			foreach (var item in enums)
			{
				objects.Add(item.ToString());
			}

			return objects;
		}
	}
}
