using Microsoft.AspNetCore.Mvc.Filters;
using MyFinanceModel;
using MyFinanceWebApiCore.Models;
using System;
using System.Linq;

namespace MyFinanceWebApiCore.Authentication
{
	public class RequiresHeaderFilterAttribute : Attribute, IActionFilter
	{
		public ServiceAppHeader RequiredServiceAppHeader { get; private set; }

		public RequiresHeaderFilterAttribute(ServiceAppHeader.ServiceAppHeaderType header)
		{
			RequiredServiceAppHeader = ServiceAppHeader.GetServiceAppHeader(header);
		}

		public void OnActionExecuted(ActionExecutedContext context)
		{
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{
			if (context.HttpContext.Request.Headers.All(h => h.Key != RequiredServiceAppHeader.Name))
			{
				throw new RequiredHeaderException(RequiredServiceAppHeader);
			}
		}
	}
}
