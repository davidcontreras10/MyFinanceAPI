using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyFinanceModel;
using MyFinanceWebApiCore.Models;
using System;
using System.Net;

namespace MyFinanceWebApiCore.FilterAttributes
{
	public class HttpResponseExceptionFilter : IActionFilter
	{
		public void OnActionExecuted(ActionExecutedContext context)
		{
			if(context.Exception is UnauthorizedAccessException unauthorizeAccessException)
			{
				var error = new
				{
					unauthorizeAccessException.Message
				};
				context.Result = new ObjectResult(error)
				{
					StatusCode = (int)HttpStatusCode.Unauthorized
				};

				context.ExceptionHandled = true;
			}
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{
		}
	}
}
