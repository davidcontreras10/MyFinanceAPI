using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System.Collections.Generic;
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
