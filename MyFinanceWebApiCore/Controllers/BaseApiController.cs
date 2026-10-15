using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MyFinanceModel;
using System;
using System.Security.Claims;

namespace MyFinanceWebApiCore.Controllers
{
	public class BaseApiController : ControllerBase
	{
		protected string GetUserId()
		{
			ValidateUserIdClaim();
			return User.FindFirst(ClaimTypes.NameIdentifier).Value;
		}

		protected void ValidateUserIdClaim()
		{
			if (string.IsNullOrWhiteSpace(User.FindFirst(ClaimTypes.NameIdentifier)?.Value))
			{
				throw new UnauthorizedAccessException();
			}
		}

		protected ApplicationModules GetModuleNameValue()
		{
			var header = ServiceAppHeader.GetServiceAppHeader(ServiceAppHeader.ServiceAppHeaderType.ApplicationModule);
			if (HttpContext.Request.Headers.ContainsKey(header.Name))
			{
				var headerValue = HttpContext.Request.Headers.TryGetValue(header.Name, out var h) ? h : StringValues.Empty;
				var module = Enum.TryParse(headerValue, out ApplicationModules r) ? r : ApplicationModules.Unknown; 
				return module;
			}

			return ApplicationModules.Unknown;
		}
	}
}
