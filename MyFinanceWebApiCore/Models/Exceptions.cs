using MyFinanceModel;
using System;
using System.Net;

namespace MyFinanceWebApiCore.Models
{
	[Serializable]
	internal class UnauthorizeAccessException : ServiceException
	{
		public UnauthorizeAccessException(string message = "User is not allowed to perform this action") : base(message,
			1, HttpStatusCode.Unauthorized)
		{
		}
	}

	internal class RequiredHeaderException : ServiceException
	{
		public RequiredHeaderException(ServiceAppHeader header) : base($"Header {header.Name} is required for this request", 1, System.Net.HttpStatusCode.BadRequest)
		{
			RequiredHeader = header;
		}

		public ServiceAppHeader RequiredHeader { get; private set; }
	}
}
