using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ExecutedTasksController : BaseApiController
	{
		private readonly IScheduledTasksService _scheduledTasksService;
		private readonly IScheduledTaskAuthorizationService _authorizationService;

		public ExecutedTasksController(IScheduledTasksService scheduledTasksService, IScheduledTaskAuthorizationService authorizationService)
		{
			_scheduledTasksService = scheduledTasksService;
			_authorizationService = authorizationService;
		}

		[HttpGet]
		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync([FromQuery] string taskId)
		{
			var permission = await _authorizationService.AuthorizeAccessAsync(GetUserId(), taskId, ResourceActionNames.View);
			if (permission?.IsAuthorized != true || permission.Scope == null)
				throw new ServiceException("Task history access is denied.", System.Net.HttpStatusCode.Forbidden);
			return await _scheduledTasksService.GetExecutedTasksByTaskIdAsync(permission.Scope);
		}
	}
}
