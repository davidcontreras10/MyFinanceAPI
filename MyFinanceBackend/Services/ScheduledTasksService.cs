using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using Serilog;

namespace MyFinanceBackend.Services
{
	public interface IScheduledTasksService
	{
		Task CreateBasicTrxAsync(
			ScheduledTaskCreateScope scope,
			ClientScheduledTask.Basic clientScheduledTask
		);

		Task CreateTransferTrxAsync(
			ScheduledTaskCreateScope scope,
			ClientScheduledTask.Transfer clientScheduledTask
		);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksByUserIdAsync(string userId);
		Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(ScheduledTaskAccessScope scope);
		Task RetireAsync(ScheduledTaskAccessScope scope);
		Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(ScheduledTaskAccessScope scope);
		Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(ScheduledTaskAccessScope scope, int skip, int limit);

		Task<TaskExecutedResult> ExecuteScheduledTaskAsync(
			ScheduledTaskExecutionScope scope,
			DateTime dateTime,
			ExecuteTaskRequestType requestType
		);

		Task<TaskExecutedResult> ExecuteDefinitionAsync(BaseScheduledTaskVm scheduledTask,
			DateTime dateTime, ExecuteTaskRequestType requestType, string userId);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync();
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetTodayScheduledTaskAsync();
		Task EditScheduledTaskAsync(ClientEditScheduledTask model, ScheduledTaskEditScope scope);
	}

	public class ScheduledTasksService : IScheduledTasksService
	{
		private readonly IAutomaticTaskRepository _automaticTaskRepository;
		private readonly IAccountRepository _accountRepository;
		private readonly ISpendsService _spendsService;
		private readonly ITransferService _transferService;
		private readonly ILogger _logger;
		private readonly IScheduledScheduleRepository _scheduleRepository;
		private readonly ScheduledExecutionOptions _executionOptions;

		public ScheduledTasksService(
			IAutomaticTaskRepository automaticTaskRepository,
			IAccountRepository accountRepository,
			ISpendsService spendsService,
			ITransferService transferService,
			ILogger logger,
			IScheduledScheduleRepository scheduleRepository = null,
			ScheduledExecutionOptions executionOptions = null
		)
		{
			_automaticTaskRepository = automaticTaskRepository;
			_accountRepository = accountRepository;
			_spendsService = spendsService;
			_transferService = transferService;
			_logger = logger;
			_scheduleRepository = scheduleRepository;
			_executionOptions = executionOptions;
		}

		public async Task<TaskExecutedResult> ExecuteScheduledTaskAsync(
			ScheduledTaskExecutionScope scope,
			DateTime dateTime,
			ExecuteTaskRequestType requestType
		)
		{
			ArgumentNullException.ThrowIfNull(scope);
			if (requestType is not (ExecuteTaskRequestType.Automatic or ExecuteTaskRequestType.Manual) ||
				(scope.ActorId != scope.OwnerId && requestType != ExecuteTaskRequestType.Automatic))
				throw new ServiceException("The execution does not match the approved scope.", System.Net.HttpStatusCode.Forbidden);
			return await _scheduleRepository.ExecuteLegacyAsync(scope, async () =>
			{
				var definitions = await _automaticTaskRepository.GetScheduledByTaskIdAsync(scope.TaskId.ToString());
				if (definitions == null || definitions.Count != 1)
					throw new ServiceException("Task not found.", System.Net.HttpStatusCode.NotFound);
				return await ExecuteDefinitionAsync(definitions.Single(), dateTime, requestType, scope.ActorId.ToString());
			});
		}

		public async Task<TaskExecutedResult> ExecuteDefinitionAsync(BaseScheduledTaskVm scheduledTask,
			DateTime dateTime, ExecuteTaskRequestType requestType, string userId)
		{
			ArgumentNullException.ThrowIfNull(scheduledTask);
			var taskId = scheduledTask.Id.ToString();

			try
			{
				TaskExecutedResult taskExecutedResult;
				switch (scheduledTask)
				{
					case BasicScheduledTaskVm baseScheduledTaskVm:
						taskExecutedResult =
							await ExecuteBasicScheduledTaskAsync(baseScheduledTaskVm, dateTime, requestType);
						break;
					case TransferScheduledTaskVm transferScheduledTaskVm:
						taskExecutedResult =
							await ExecuteTransferScheduledTaskAsync(transferScheduledTaskVm, dateTime, requestType);
						break;
					default:
						taskExecutedResult = TaskExecutedResult.Error("Task not supported", taskId);
						break;
				}

				await RecordExecutedTaskAsync(scheduledTask, taskExecutedResult, dateTime, userId);
				return taskExecutedResult;
			}
			catch (Exception e)
			{
				_logger.Error(e.ToString());
				throw;
			}
		}

		public async Task CreateBasicTrxAsync(
			ScheduledTaskCreateScope scope,
			ClientScheduledTask.Basic clientScheduledTask
		)
		{
			await CreateAsync(scope, clientScheduledTask);
		}

		public async Task CreateTransferTrxAsync(
			ScheduledTaskCreateScope scope,
			ClientScheduledTask.Transfer clientScheduledTask
		)
		{
			await CreateAsync(scope, clientScheduledTask);
		}

		private async Task CreateAsync(ScheduledTaskCreateScope scope, ClientScheduledTask model)
		{
			ArgumentNullException.ThrowIfNull(scope);
			ScheduledExecutionRules.ValidateCreation(model);
			if (_executionOptions?.HistoryEnabled == true)
				await _scheduleRepository.RegisterAsync(scope.OwnerId, () => _automaticTaskRepository.CreateAsync(scope, model));
			else
				await _automaticTaskRepository.CreateAsync(scope, model);
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetTodayScheduledTaskAsync()
		{
			var scheduledTasks = await _automaticTaskRepository.GetScheduledTasksAsync();
			var today = DateTime.UtcNow;
			return scheduledTasks.Where(t => IsTaskForToday(t, today)).ToList();
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync()
		{
			return await _automaticTaskRepository.GetScheduledTasksAsync();
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksByUserIdAsync(string userId)
		{
			return await _automaticTaskRepository.GetScheduledByUserIdAsync(userId);
		}

		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(ScheduledTaskAccessScope scope)
		{
			ArgumentNullException.ThrowIfNull(scope);
			return await _automaticTaskRepository.GetExecutedTasksByTaskIdAsync(scope);
		}

		public Task RetireAsync(ScheduledTaskAccessScope scope) => _scheduleRepository.RetireAsync(scope);
		public Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(ScheduledTaskAccessScope scope) =>
			_scheduleRepository.GetExecutionContextAsync(scope);
		public Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(ScheduledTaskAccessScope scope, int skip, int limit) =>
			_scheduleRepository.GetExecutionHistoryAsync(scope, skip, limit);

		public async Task EditScheduledTaskAsync(ClientEditScheduledTask model, ScheduledTaskEditScope scope)
		{
			ArgumentNullException.ThrowIfNull(model);
			ArgumentNullException.ThrowIfNull(scope);
			if (_executionOptions?.HistoryEnabled == true)
			{
				await _scheduleRepository.ReviseAsync(scope.TaskId, scope.OwnerId,
					() => _automaticTaskRepository.EditScheduledTaskAsync(model, scope));
				return;
			}
			await _automaticTaskRepository.EditScheduledTaskAsync(model, scope);
		}

		private bool IsTaskForToday(BaseScheduledTaskVm baseScheduledTaskVm, DateTime today)
		{
			if (!baseScheduledTaskVm.Days.Any())
			{
				return false;
			}

			return baseScheduledTaskVm.FrequencyType switch
			{
				ScheduledTaskFrequencyType.Invalid => false,
				ScheduledTaskFrequencyType.Manual => false,
				ScheduledTaskFrequencyType.Monthly => baseScheduledTaskVm.Days.Contains(GetDayOfMonth(today)),
				ScheduledTaskFrequencyType.Weekly => baseScheduledTaskVm.Days.Contains(GetDayOfWeek(today)),
				_ => throw new ArgumentOutOfRangeException(),
			};
		}

		private int GetDayOfWeek(DateTime dateTime)
		{
			return (int) dateTime.DayOfWeek;
		}

		private int GetDayOfMonth(DateTime dateTime)
		{
			return dateTime.Day;
		}

		private async Task RecordExecutedTaskAsync(
			BaseScheduledTaskVm scheduledTaskVm,
			TaskExecutedResult taskExecutedResult,
			DateTime dateTime,
			string userId
		)
		{
			if (scheduledTaskVm == null)
			{
				throw new ArgumentNullException(nameof(scheduledTaskVm));
			}

			if (taskExecutedResult == null)
			{
				throw new ArgumentNullException(nameof(taskExecutedResult));
			}

			var clientExecutedTask = new ClientExecutedTask
			{
				AutomaticTaskId = scheduledTaskVm.Id.ToString(),
				ExecuteDatetime = dateTime,
				ExecutionStatus = taskExecutedResult.Status,
				ExecutionMsg = taskExecutedResult.ErrorMsg,
				ExecutedByUserId = userId
			};

			await _automaticTaskRepository.RecordClientExecutedTaskAsync(clientExecutedTask);
		}

		private async Task<TaskExecutedResult> ExecuteTransferScheduledTaskAsync(
			TransferScheduledTaskVm transferScheduledTask,
			DateTime dateTime,
			ExecuteTaskRequestType requestType
		)
		{
			if (transferScheduledTask == null)
			{
				throw new ArgumentNullException(nameof(transferScheduledTask));
			}

			if (transferScheduledTask.AccountId == 0)
			{
				return TaskExecutedResult.Error("Invalid accountId", transferScheduledTask.Id.ToString());
			}

			if (transferScheduledTask.ToAccountId == 0)
			{
				return TaskExecutedResult.Error("Invalid to accountId", transferScheduledTask.Id.ToString());
			}

			var currentAccountPeriod =
				await _accountRepository.GetAccountPeriodInfoByAccountIdDateTimeAsync(transferScheduledTask.AccountId,
					dateTime);
			var transferRequest = new TransferClientViewModel
			{
				SpendDate = dateTime,
				Description = GetExecuteTaskDescription(transferScheduledTask.Description, requestType,
					transferScheduledTask.TaskType),
				AccountPeriodId = currentAccountPeriod.AccountPeriodId,
				SpendTypeId = transferScheduledTask.SpendTypeId,
				CurrencyId = transferScheduledTask.CurrencyId,
				IsPending = transferScheduledTask.IsPending,
				Amount = transferScheduledTask.Amount,
				UserId = currentAccountPeriod.UserId,
				AmountTypeId = TransactionTypeIds.Ignore,
				BalanceType = BalanceTypes.Custom,
				DestinationAccount = transferScheduledTask.ToAccountId
			};

			await _transferService.SubmitTransferAsync(transferRequest);
			return TaskExecutedResult.Success(transferScheduledTask.Id.ToString());
		}

		private async Task<TaskExecutedResult> ExecuteBasicScheduledTaskAsync(
			BasicScheduledTaskVm basicScheduledTaskVm,
			DateTime dateTime,
			ExecuteTaskRequestType requestType
		)
		{
			if (basicScheduledTaskVm == null)
			{
				throw new ArgumentNullException(nameof(basicScheduledTaskVm));
			}

			if (basicScheduledTaskVm.AccountId == 0)
			{
				return TaskExecutedResult.Error("Invalid accountId", basicScheduledTaskVm.Id.ToString());
			}

			var currentAccountPeriod =
				await _accountRepository.GetAccountPeriodInfoByAccountIdDateTimeAsync(basicScheduledTaskVm.AccountId,
					dateTime);
			if (currentAccountPeriod == null || currentAccountPeriod.AccountPeriodId == 0)
			{
				return TaskExecutedResult.Error("No current period", basicScheduledTaskVm.Id.ToString());
			}

			var basicTrxCreate = new ClientBasicTrxByPeriod
			{
				AmountTypeId = basicScheduledTaskVm.IsSpend ? TransactionTypeIds.Spend : TransactionTypeIds.Saving,
				SpendDate = dateTime,
				Description = GetExecuteTaskDescription(basicScheduledTaskVm.Description, requestType,
					basicScheduledTaskVm.TaskType),
				AccountPeriodId = currentAccountPeriod.AccountPeriodId,
				SpendTypeId = basicScheduledTaskVm.SpendTypeId,
				CurrencyId = basicScheduledTaskVm.CurrencyId,
				IsPending = basicScheduledTaskVm.IsPending,
				Amount = basicScheduledTaskVm.Amount,
				UserId = currentAccountPeriod.UserId,
			};

			await _spendsService.AddBasicTransactionAsync(basicTrxCreate, basicTrxCreate.AmountTypeId);
			return TaskExecutedResult.Success(basicScheduledTaskVm.Id.ToString());
		}

		private static string GetExecuteTaskDescription(
			string baseDescription,
			ExecuteTaskRequestType requestType,
			ScheduledTaskType scheduledTaskType
		)
		{
			baseDescription = string.IsNullOrWhiteSpace(baseDescription) ? "No Desc" : baseDescription;
			return $"AUTO_TASK_{scheduledTaskType}_{baseDescription}_{requestType}";
		}
	}
}
