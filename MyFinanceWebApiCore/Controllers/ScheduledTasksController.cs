using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using MyFinanceWebApiCore.Authentication;

namespace MyFinanceWebApiCore.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ScheduledTasksController : BaseApiController
	{
		private readonly IScheduledTasksService _scheduledTasksService;

		public ScheduledTasksController(IScheduledTasksService scheduledTasksService)
		{
			_scheduledTasksService = scheduledTasksService;
		}

		[Route("basic")]
		[HttpPost]
		public async Task CreateBasicAsync(ClientScheduledTask.Basic model)
		{
			var userId = GetUserId();
			await _scheduledTasksService.CreateBasicTrxAsync(userId, model);
		}

		[Route("transfer")]
		[HttpPost]
		public async Task CreateTransferAsync(ClientScheduledTask.Transfer model)
		{
			var userId = GetUserId();
			await _scheduledTasksService.CreateTransferTrxAsync(userId, model);
		}

		[AdminRequired]
		[Route("today")]
		[HttpGet]
		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetTodayScheduledTaskAsync()
		{
			return await _scheduledTasksService.GetTodayScheduledTaskAsync();
		}

		[AdminRequired]
		[Route("")]
		[HttpGet]
		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetAllScheduledTaskAsync()
		{
			return await _scheduledTasksService.GetScheduledTasksAsync();
		}

		[Route("@current")]
		[HttpGet]
		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTaskAsync()
		{
			var userId = GetUserId();
			return await _scheduledTasksService.GetScheduledTasksByUserIdAsync(userId);
		}

		[HttpDelete]
		[Route("{taskId}")]
		public async Task DeleteScheduledTaskAsync(string taskId)
		{
			await _scheduledTasksService.DeleteByIdAsync(taskId);
		}

		[HttpPatch]
		[Route("{taskId}")]
		public async Task EditScheduledTaskAsync(string taskId, ClientEditScheduledTask model)
		{
			ArgumentNullException.ThrowIfNull(model);
			var userId = GetUserId();
			await EnsureTaskBelongsToUserAsync(taskId, userId);
			model.TaskId = taskId;
			await _scheduledTasksService.EditScheduledTaskAsync(model);
		}

		private async Task EnsureTaskBelongsToUserAsync(string taskId, string userId)
		{
			var userTasks = await _scheduledTasksService.GetScheduledTasksByUserIdAsync(userId);
			var belongsToUser = userTasks.Any(t => string.Equals(t.Id.ToString(), taskId, StringComparison.OrdinalIgnoreCase));
			if (!belongsToUser)
			{
				throw new UnauthorizedAccessException($"ScheduledTask {taskId} does not belong to the current user");
			}
		}

		[HttpPost]
		[Route("{taskId}/execution")]
		public async Task<TaskExecutedResult> ExecuteAutomaticTaskAsync(
			string taskId,
			ClientExecuteTask clientExecuteTask

		)
		{
			clientExecuteTask.DateTime = clientExecuteTask.DateTime ?? DateTime.Now;
			return await _scheduledTasksService.ExecuteScheduledTaskAsync(taskId, clientExecuteTask.DateTime.Value,
				clientExecuteTask.RequestType,
				GetUserId());
		}
	}
}
