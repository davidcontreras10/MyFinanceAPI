using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyFinanceModel;
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
			else if (context.Exception is ServiceException serviceException)
			{
				var error = new
				{
					serviceException.Message,
					serviceException.ErrorCode
				};
				context.Result = new ObjectResult(error)
				{
					StatusCode = (int)serviceException.StatusCode
				};

				context.ExceptionHandled = true;
			}
			else if (context.Exception is Exception exception)
			{
				var error = new
				{
					exception.Message
				};
				context.Result = new ObjectResult(error)
				{
					StatusCode = (int)HttpStatusCode.InternalServerError
				};

				context.ExceptionHandled = true;
			}
		}

		public void OnActionExecuting(ActionExecutingContext context)
		{
		}
	}
}
