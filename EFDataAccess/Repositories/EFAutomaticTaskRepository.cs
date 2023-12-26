using EFDataAccess.Helpers;
using EFDataAccess.Models;
using EFDataAccess.Models.Customs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFAutomaticTaskRepository : BaseEFRepository, IAutomaticTaskRepository
	{
		private readonly ILogger<EFAutomaticTaskRepository> _logger;

		public EFAutomaticTaskRepository(MyFinanceContext context, ILogger<EFAutomaticTaskRepository> logger) : base(context)
		{
			_logger = logger;
		}

		public void BeginTransaction()
		{
			Context.Database.BeginTransaction();
		}

		public void Commit()
		{
			Context.Database.CommitTransaction();
		}

		public async Task DeleteByIdAsync(string taskId)
		{
			try
			{
				var taskGuid = new Guid(taskId);

				var executedTasks = Context.ExecutedTask.Where(x => x.AutomaticTaskId == taskGuid);
				Context.ExecutedTask.RemoveRange(executedTasks);

				var spInTrxDefs = Context.SpInTrxDef.Where(x => x.SpInTrxDefId == taskGuid);
				Context.SpInTrxDef.RemoveRange(spInTrxDefs);

				var transferTrxDefs = Context.TransferTrxDef.Where(x => x.TransferTrxDefId == taskGuid);
				Context.TransferTrxDef.RemoveRange(transferTrxDefs);

				var automaticTasks = Context.AutomaticTask.Where(x => x.AutomaticTaskId == taskGuid);
				Context.AutomaticTask.RemoveRange(automaticTasks);

				await Context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error on DeleteByIdAsync");
				throw ex;
			}
		}

		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(string taskId)
		{
			return await Context.ExecutedTask.AsNoTracking()
				.Where(x => x.AutomaticTaskId == new Guid(taskId))
				.Select(x => new ExecutedTaskViewModel
				{
					ExecutedDate = x.ExecuteDatetime,
					Message = x.ExecutionMsg,
					Status = (ExecutedTaskStatus)x.ExecutionStatus
				})
				.OrderByDescending(x => x.ExecutedDate)
				.ToListAsync();
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByTaskIdAsync(string taskId)
		{
			return await GetAutomaticTasksAsync(taskId: taskId);
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByUserIdAsync(string userId)
		{
			return await GetAutomaticTasksAsync(userId: userId);
		}

		public async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync()
		{
			return await GetAutomaticTasksAsync();
		}

		public async Task InsertBasicTrxAsync(string userId, ClientScheduledTask.Basic clientScheduledTask)
		{
			var taskId = Guid.NewGuid();
			var userGuid = new Guid(userId);
			var spInTrxDef = new SpInTrxDef
			{
				SpInTrxDefNavigation = CreateAutomaticTask(clientScheduledTask, taskId, userGuid),
				IsSpendTrx = clientScheduledTask.IsSpendTrx,
				SpInTrxDefId = taskId
			};

			await Context.SpInTrxDef.AddAsync(spInTrxDef);
			await Context.SaveChangesAsync();
		}

		public async Task InsertTransferTrxAsync(string userId, ClientScheduledTask.Transfer clientScheduledTask)
		{
			var taskId = Guid.NewGuid();
			var userGuid = new Guid(userId);
			var transferTrxDef = new TransferTrxDef
			{
				TransferTrxDefNavigation = CreateAutomaticTask(clientScheduledTask, taskId, userGuid),
				TransferTrxDefId = taskId,
				ToAccountId = clientScheduledTask.ToAccountId
			};

			await Context.TransferTrxDef.AddAsync(transferTrxDef);
			await Context.SaveChangesAsync();
		}

		public async Task RecordClientExecutedTaskAsync(ClientExecutedTask clientExecutedTask)
		{
			var executedTask = new ExecutedTask
			{
				AutomaticTaskId = clientExecutedTask.AutomaticTaskId.ToGuid(),
				ExecuteDatetime = clientExecutedTask.ExecuteDatetime,
				ExecutedByUserId = clientExecutedTask.ExecutedByUserId.ToGuid(),
				ExecutionMsg = clientExecutedTask.ExecutionMsg,
				ExecutionStatus = (int)clientExecutedTask.ExecutionStatus
			};

			await Context.AddAsync(executedTask);
			await Context.SaveChangesAsync();
		}

		public void RollbackTransaction()
		{
			Context.Database.RollbackTransaction();
		}

		private async Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetAutomaticTasksAsync(string userId = null, string taskId = null)
		{
			if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(taskId))
			{
				throw new ArgumentException($"Must pass either {nameof(userId)} or {nameof(taskId)} but not both");
			}

			try
			{
				var fullQuery = string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(taskId);
				var userGuid = !string.IsNullOrEmpty(userId) ? new Guid(userId) : Guid.Empty;
				var taskGuid = !string.IsNullOrEmpty(taskId) ? new Guid(taskId) : Guid.Empty;
				var spInTrxTasks = await Context.SpInTrxDef.AsNoTracking()
					.Include(x => x.SpInTrxDefNavigation)
						.ThenInclude(x => x.Account)
					.Include(x => x.SpInTrxDefNavigation)
						.ThenInclude(x => x.Currency)
					.Include(x => x.SpInTrxDefNavigation)
						.ThenInclude(x => x.ExecutedTask)
					.Where(x => fullQuery || x.SpInTrxDefNavigation.UserId == userGuid || x.SpInTrxDefNavigation.AutomaticTaskId == taskGuid)
					.ToListAsync();
				FilterLatestExecutedTask(spInTrxTasks);
				var transferTasks = await Context.TransferTrxDef.AsNoTracking()
					.Include(x => x.TransferTrxDefNavigation)
						.ThenInclude(x => x.Account)
					.Include(x => x.TransferTrxDefNavigation)
						.ThenInclude(x => x.Currency)
					.Include(x => x.TransferTrxDefNavigation)
						.ThenInclude(x => x.ExecutedTask)
					.Include(x => x.ToAccount)
					.Where(x => fullQuery || x.TransferTrxDefNavigation.UserId == userGuid || x.TransferTrxDefNavigation.AutomaticTaskId == taskGuid)
					.ToListAsync();
				FilterLatestExecutedTask(transferTasks);
				var scheduledTasks = new List<BaseScheduledTaskVm>();
				scheduledTasks.AddRange(spInTrxTasks.Select(x => ToBaseScheduledTaskVm(x)));
				scheduledTasks.AddRange(transferTasks.Select(x => ToBaseScheduledTaskVm(x)));
				return scheduledTasks;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "GetAutomaticTasksAsync error");
				throw;
			}

		}

		private static bool AutomaticTaskMatches(AutomaticTask automaticTask, Guid userId, Guid taskId, bool fullQuery)
		{
			return (fullQuery || automaticTask.UserId == userId || automaticTask.AutomaticTaskId == taskId);
		}

		private static AutomaticTask CreateAutomaticTask(ClientScheduledTask clientScheduledTask, Guid id, Guid userId)
		{
			return new AutomaticTask
			{
				AccountId = clientScheduledTask.AccountId,
				Amount = clientScheduledTask.Amount,
				AutomaticTaskId = id,
				CurrencyId = clientScheduledTask.CurrencyId,
				Days = ToStringCharSeparated(clientScheduledTask.Days),
				PeriodTypeId = clientScheduledTask.FrequencyType,
				SpendTypeId = clientScheduledTask.SpendTypeId,
				UserId = userId,
				TaskDescription = clientScheduledTask.Description
			};
		}

		private static BaseScheduledTaskVm ToBaseScheduledTaskVm<T>(T automaticTaskDef) where T : IAutomaticTaskDef
		{
			BaseScheduledTaskVm scheduledTaskVm;

			if (automaticTaskDef is SpInTrxDef spInTrxDef)
			{
				scheduledTaskVm = new BasicScheduledTaskVm
				{
					IsSpend = spInTrxDef.IsSpendTrx
				};
			}
			else if (automaticTaskDef is TransferTrxDef transferTrxDef)
			{
				scheduledTaskVm = new TransferScheduledTaskVm
				{
					ToAccountId = transferTrxDef.ToAccountId,
					ToAccountName = transferTrxDef.ToAccount.Name
				};
			}
			else
			{
				throw new ArgumentException("Invalid type", nameof(automaticTaskDef));
			}

			scheduledTaskVm.Id = automaticTaskDef.TrxDefId;
			scheduledTaskVm.Amount = (float)automaticTaskDef.AutomaticTaskNavigation.Amount;
			scheduledTaskVm.SpendTypeId = automaticTaskDef.AutomaticTaskNavigation.SpendTypeId;
			scheduledTaskVm.CurrencyId = automaticTaskDef.AutomaticTaskNavigation.CurrencyId;
			scheduledTaskVm.CurrencySymbol = automaticTaskDef.AutomaticTaskNavigation.Currency.Symbol;
			scheduledTaskVm.Description = automaticTaskDef.AutomaticTaskNavigation.TaskDescription;
			scheduledTaskVm.AccountId = automaticTaskDef.AutomaticTaskNavigation.AccountId;
			scheduledTaskVm.AccountName = automaticTaskDef.AutomaticTaskNavigation.Account.Name;
			scheduledTaskVm.FrequencyType = (ScheduledTaskFrequencyType)automaticTaskDef.AutomaticTaskNavigation.PeriodTypeId;
			scheduledTaskVm.Days = CreateArrayFromStringArray(automaticTaskDef.AutomaticTaskNavigation.Days);
			if (automaticTaskDef.AutomaticTaskNavigation.ExecutedTask.FirstOrDefault() is ExecutedTask executedTask)
			{
				scheduledTaskVm.LastExecutedStatus = (ExecutedTaskStatus)executedTask.ExecutionStatus;
				scheduledTaskVm.LastExecutedMsg = executedTask.ExecutionMsg;
			}

			return scheduledTaskVm;
		}


		private static string ToStringCharSeparated(IEnumerable<int> ids, char separator = ',')
		{
			if (ids == null)
				return string.Empty;
			var result = ids.Aggregate(string.Empty, (current, i) => current + ("," + i.ToString(CultureInfo.InvariantCulture)));
			if (!string.IsNullOrEmpty(result) && result[0] == separator)
				result = result.Remove(0, 1);
			return result;
		}

		private static void FilterLatestExecutedTask<T>(IReadOnlyCollection<T> tasks) where T : IAutomaticTaskDef
		{
			if (!tasks.Any())
			{
				return;
			}

			foreach (var task in tasks.Where(t => t.AutomaticTaskNavigation?.ExecutedTask != null && t.AutomaticTaskNavigation.ExecutedTask.Any()))
			{
				task.AutomaticTaskNavigation.ExecutedTask = new[] {
					task.AutomaticTaskNavigation.ExecutedTask
						.OrderByDescending(et => et.ExecuteDatetime)
						.FirstOrDefault()
				};
			}
		}

		private static int[] CreateArrayFromStringArray(string stringArray)
		{
			if (string.IsNullOrEmpty(stringArray))
			{
				return Array.Empty<int>();
			}

			stringArray = stringArray.Trim();
			var arrayValue = stringArray.Split(',');
			var list = new List<int>();
			foreach (var s in arrayValue)
			{
				int result;
				result = int.TryParse(s, out result) ? result : 0;
				if (result > 0)
				{
					list.Add(result);
				}
			}
			return list.ToArray();
		}
	}
}
