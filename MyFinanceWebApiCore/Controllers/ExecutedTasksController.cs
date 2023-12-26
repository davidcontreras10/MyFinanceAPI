using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
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

		public ExecutedTasksController(IScheduledTasksService scheduledTasksService)
		{
			_scheduledTasksService = scheduledTasksService;
		}

		[HttpGet]
		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync([FromQuery] string taskId)
		{
			return await _scheduledTasksService.GetExecutedTasksByTaskIdAsync(taskId);
		}
	}
}
