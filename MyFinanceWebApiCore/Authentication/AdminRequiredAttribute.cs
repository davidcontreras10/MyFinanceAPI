using Microsoft.AspNetCore.Mvc.Filters;
using MyFinanceWebApiCore.Models;
using System;

namespace MyFinanceWebApiCore.Authentication
{
	public class AdminRequiredAttribute : Attribute, IActionFilter
	{
		public void OnActionExecuted(ActionExecutedContext context)
		{
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{
			if (!context.HttpContext.User.IsInRole("Admin"))
			{
				throw new UnauthorizeAccessException("Admin user required");
			}
		}
	}
}
