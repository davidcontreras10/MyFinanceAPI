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
			string userId,
			ClientScheduledTask.Basic clientScheduledTask
		);

		Task CreateTransferTrxAsync(
			string userId,
			ClientScheduledTask.Transfer clientScheduledTask
		);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksByUserIdAsync(string userId);
		Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(string taskId);
		Task DeleteByIdAsync(string taskId);

		Task<TaskExecutedResult> ExecuteScheduledTaskAsync(
			string taskId,
			DateTime dateTime,
			ExecuteTaskRequestType requestType,
			string userId
		);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync();
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetTodayScheduledTaskAsync();
	}

	public class ScheduledTasksService : IScheduledTasksService
	{
		private readonly IAutomaticTaskRepository _automaticTaskRepository;
		private readonly IAccountRepository _accountRepository;
		private readonly ISpendsService _spendsService;
		private readonly ITransferService _transferService;
		private readonly ILogger _logger;

		public ScheduledTasksService(
			IAutomaticTaskRepository automaticTaskRepository,
			IAccountRepository accountRepository,
			ISpendsService spendsService,
			ITransferService transferService,
			ILogger logger
		)
		{
			_automaticTaskRepository = automaticTaskRepository;
			_accountRepository = accountRepository;
			_spendsService = spendsService;
			_transferService = transferService;
			_logger = logger;
		}

		public async Task<TaskExecutedResult> ExecuteScheduledTaskAsync(
			string taskId,
			DateTime dateTime,
			ExecuteTaskRequestType requestType,
			string userId
		)
		{
			var scheduledTasks = await _automaticTaskRepository.GetScheduledByTaskIdAsync(taskId);
			if (scheduledTasks == null || !scheduledTasks.Any())
			{
				return TaskExecutedResult.Error($"TaskId: {taskId} does not exist", taskId);
			}

			try
			{
				TaskExecutedResult taskExecutedResult;
				var scheduledTask = scheduledTasks.First();
				//_automaticTaskRepository.BeginTransaction();
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
				//_automaticTaskRepository.Commit();
				return taskExecutedResult;
			}
			catch (Exception e)
			{
				_logger.Error(e.ToString());
				//_automaticTaskRepository.RollbackTransaction();
				throw;
			}
		}

		public async Task CreateBasicTrxAsync(
			string userId,
			ClientScheduledTask.Basic clientScheduledTask
		)
		{
			await _automaticTaskRepository.InsertBasicTrxAsync(userId, clientScheduledTask);
		}

		public async Task CreateTransferTrxAsync(
			string userId,
			ClientScheduledTask.Transfer clientScheduledTask
		)
		{
			await _automaticTaskRepository.InsertTransferTrxAsync(userId, clientScheduledTask);
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

		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(string taskId)
		{
			return await _automaticTaskRepository.GetExecutedTasksByTaskIdAsync(taskId);
		}

		public async Task DeleteByIdAsync(string taskId)
		{
			await _automaticTaskRepository.DeleteByIdAsync(taskId);
		}

		private bool IsTaskForToday(BaseScheduledTaskVm baseScheduledTaskVm, DateTime today)
		{
			if (!baseScheduledTaskVm.Days.Any())
			{
				return false;
			}

			switch (baseScheduledTaskVm.FrequencyType)
			{
				case ScheduledTaskFrequencyType.Invalid:
					return false;
				case ScheduledTaskFrequencyType.Monthly:
					return baseScheduledTaskVm.Days.Contains(GetDayOfMonth(today));
				case ScheduledTaskFrequencyType.Weekly:
					return baseScheduledTaskVm.Days.Contains(GetDayOfWeek(today));
				default:
					throw new ArgumentOutOfRangeException();
			}
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
				IsPending = false,
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
				IsPending = false,
				Amount = basicScheduledTaskVm.Amount,
				UserId = currentAccountPeriod.UserId
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
