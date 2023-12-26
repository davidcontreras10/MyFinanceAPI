using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using MyFinanceModel;
using System;
using System.Linq;
using System.Security.Claims;

namespace MyFinanceWebApiCore.Controllers
{
	public class BaseApiController : ControllerBase
	{
		protected string GetUserId()
		{
			var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
			return userIdClaim.Value;
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
