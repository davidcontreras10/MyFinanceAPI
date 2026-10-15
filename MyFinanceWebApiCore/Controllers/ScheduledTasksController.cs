using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
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
		private const int DefaultHistoryPageSize = 50;

		private readonly IScheduledTasksService _scheduledTasksService;
		private readonly IScheduledTaskAuthorizationService _authorizationService;

		public ScheduledTasksController(IScheduledTasksService scheduledTasksService,
			IScheduledTaskAuthorizationService authorizationService)
		{
			_scheduledTasksService = scheduledTasksService;
			_authorizationService = authorizationService;
		}

		[Route("basic")]
		[HttpPost]
		public async Task CreateBasicAsync(ClientScheduledTask.Basic model)
		{
			var scope = await AuthorizeCreationAsync(model);
			await _scheduledTasksService.CreateBasicTrxAsync(scope, model);
		}

		[Route("transfer")]
		[HttpPost]
		public async Task CreateTransferAsync(ClientScheduledTask.Transfer model)
		{
			var scope = await AuthorizeCreationAsync(model);
			await _scheduledTasksService.CreateTransferTrxAsync(scope, model);
		}

		private async Task<ScheduledTaskCreateScope> AuthorizeCreationAsync(ClientScheduledTask model)
		{
			var result = await _authorizationService.AuthorizeCreateAsync(GetUserId(), model);
			if (result?.IsAuthorized != true || result.Scope == null)
				throw new ServiceException("Scheduled-task creation access is denied.", System.Net.HttpStatusCode.Forbidden);
			return result.Scope;
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
			await _scheduledTasksService.RetireAsync(await AuthorizeAccessAsync(taskId, ResourceActionNames.Delete));
		}

		[HttpGet("{taskId}/execution-context")]
		public async Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(string taskId) =>
			await _scheduledTasksService.GetExecutionContextAsync(await AuthorizeAccessAsync(taskId, ResourceActionNames.View));

		[HttpGet("{taskId}/execution-history")]
		public async Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(string taskId, [FromQuery] int skip = 0, [FromQuery] int limit = DefaultHistoryPageSize) =>
			await _scheduledTasksService.GetExecutionHistoryAsync(await AuthorizeAccessAsync(taskId, ResourceActionNames.View), skip, limit);

		private async Task<ScheduledTaskAccessScope> AuthorizeAccessAsync(string taskId, ResourceActionNames action)
		{
			var result = await _authorizationService.AuthorizeAccessAsync(GetUserId(), taskId, action);
			if (result?.IsAuthorized != true || result.Scope == null)
				throw new ServiceException("Scheduled-task access is denied.", System.Net.HttpStatusCode.Forbidden);
			return result.Scope;
		}

		[HttpPatch]
		[Route("{taskId}")]
		public async Task EditScheduledTaskAsync(string taskId, ClientEditScheduledTask model)
		{
			var userId = GetUserId();
			ArgumentNullException.ThrowIfNull(model);
			var result = await _authorizationService.AuthorizeEditAsync(userId, taskId,
				model.ModifyList?.Contains(ClientEditScheduledTask.ScheduledTaskField.SpendTypeId) == true
					? model.SpendTypeId : null);
			if (result?.IsAuthorized != true || result.Scope == null)
				throw new ServiceException("Scheduled-task edit access is denied.", System.Net.HttpStatusCode.Forbidden);
			model.TaskId = result.Scope.TaskId.ToString();
			await _scheduledTasksService.EditScheduledTaskAsync(model, result.Scope);
		}

		[HttpPost]
		[Route("{taskId}/execution")]
		public async Task<TaskExecutedResult> ExecuteAutomaticTaskAsync(
			string taskId,
			ClientExecuteTask clientExecuteTask

		)
		{
			var actor = GetUserId();
			ArgumentNullException.ThrowIfNull(clientExecuteTask);
			var permission = await _authorizationService.AuthorizeExecutionAsync(actor, taskId,
				clientExecuteTask.RequestType == ExecuteTaskRequestType.Automatic);
			if (permission?.IsAuthorized != true || permission.Scope == null)
				throw new ServiceException("Task execution access is denied.", System.Net.HttpStatusCode.Forbidden);
			clientExecuteTask.DateTime = clientExecuteTask.DateTime ?? DateTime.Now;
			return await _scheduledTasksService.ExecuteScheduledTaskAsync(permission.Scope, clientExecuteTask.DateTime.Value,
				clientExecuteTask.RequestType);
		}
	}
}
