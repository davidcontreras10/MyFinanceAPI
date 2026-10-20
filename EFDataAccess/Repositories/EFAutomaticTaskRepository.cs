using EFDataAccess.Helpers;
using EFDataAccess.Models;
using EFDataAccess.Models.Customs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EFDataAccess.Repositories
{
	public class EFAutomaticTaskRepository : BaseEFRepository, IAutomaticTaskRepository, IScheduledTaskAccessRepository
	{
		private const string AdministratorRoleName = "Admin";

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

		public async Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(ScheduledTaskAccessScope scope)
		{
			ArgumentNullException.ThrowIfNull(scope);
			if (scope.Action != ResourceActionNames.View)
				throw new ServiceException("History access is denied.", HttpStatusCode.Forbidden);
			return await Context.ExecutedTask.AsNoTracking()
				.Where(x => x.AutomaticTaskId == scope.TaskId && Context.AutomaticTask.Any(task =>
					task.AutomaticTaskId == x.AutomaticTaskId && task.UserId == scope.OwnerId))
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

		public async Task<Guid> CreateAsync(ScheduledTaskCreateScope scope, ClientScheduledTask model)
		{
			ArgumentNullException.ThrowIfNull(scope);
			MyFinanceBackend.Services.ScheduledExecutionRules.ValidateCreation(model);
			await using var transaction = Context.Database.CurrentTransaction == null
				? await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
			var accounts = model is ClientScheduledTask.Transfer transfer
				? new[] { model.AccountId, transfer.ToAccountId }.Distinct().ToArray() : new[] { model.AccountId };
			if (await Context.Account.CountAsync(x => x.UserId == scope.OwnerId && accounts.Contains(x.AccountId)) != accounts.Length ||
				!await Context.UserSpendType.AnyAsync(x => x.UserId == scope.OwnerId && x.SpendTypeId == model.SpendTypeId))
				throw new ServiceException("Task references are outside the approved owner scope.", HttpStatusCode.NotFound);
			var id = Guid.NewGuid();
			var task = CreateAutomaticTask(model, id, scope.OwnerId);
			if (model is ClientScheduledTask.Basic basic)
				task.SpInTrxDef = new SpInTrxDef { SpInTrxDefId = id, IsSpendTrx = basic.IsSpendTrx };
			else
				task.TransferTrxDef = new TransferTrxDef { TransferTrxDefId = id, ToAccountId = ((ClientScheduledTask.Transfer)model).ToAccountId };
			Context.Add(task);
			await Context.SaveChangesAsync();
			if (transaction != null) await transaction.CommitAsync();
			return id;
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

		public Task<bool> IsOwnedByAsync(Guid taskId, Guid ownerId) =>
			Context.AutomaticTask.AsNoTracking().AnyAsync(x => taskId != Guid.Empty && ownerId != Guid.Empty &&
				x.AutomaticTaskId == taskId && x.UserId == ownerId);

		public Task<Guid?> GetOwnerIdAsync(Guid taskId) => Context.AutomaticTask.AsNoTracking()
			.Where(x => x.AutomaticTaskId == taskId).Select(x => (Guid?)x.UserId).SingleOrDefaultAsync();

		public Task<bool> IsAdministratorAsync(Guid actorId) => Context.AppUser.AnyAsync(x =>
			x.UserId == actorId && x.UserRoles.Any(role => role.Name == AdministratorRoleName));

		public async Task EditScheduledTaskAsync(ClientEditScheduledTask model, ScheduledTaskEditScope scope)
		{
			ArgumentNullException.ThrowIfNull(scope);
			if (model == null || model.ModifyList == null || !model.ModifyList.Any())
			{
				throw new ArgumentException("Invalid parameters", nameof(model));
			}

			var modifyList = model.ModifyList.ToList();
			var changingFrequencyType = modifyList.Contains(ClientEditScheduledTask.ScheduledTaskField.FrequencyType);
			var changingDays = modifyList.Contains(ClientEditScheduledTask.ScheduledTaskField.Days);
			if (changingFrequencyType && model.FrequencyType != ScheduledTaskFrequencyType.Manual && !changingDays)
			{
				throw new ServiceException("Days must be provided when modifying FrequencyType to Monthly or Weekly", HttpStatusCode.BadRequest);
			}

			if (changingDays && (model.Days == null || !model.Days.Any()))
			{
				throw new ServiceException("Days cannot be empty", HttpStatusCode.BadRequest);
			}

			await using var transaction = Context.Database.CurrentTransaction == null
				? await Context.Database.BeginTransactionAsync(IsolationLevel.Serializable) : null;
			// Keep the approved owner constraint stable until the enclosing edit commits.
			var automaticTask = await Context.AutomaticTask.FromSqlInterpolated(
				$"SELECT * FROM [AutomaticTask] WITH (UPDLOCK, HOLDLOCK) WHERE [AutomaticTaskId] = {scope.TaskId} AND [UserId] = {scope.OwnerId}")
				.SingleOrDefaultAsync();
			if (automaticTask == null)
			{
				throw new ServiceException("Scheduled task not found within the approved scope.", HttpStatusCode.NotFound);
			}
			if (automaticTask.RetiredAtUtc.HasValue)
				throw new ServiceException("A retired task cannot be edited.", HttpStatusCode.Conflict);
			if (modifyList.Contains(ClientEditScheduledTask.ScheduledTaskField.SpendTypeId) &&
				(model.SpendTypeId <= 0 || !await Context.UserSpendType.FromSqlInterpolated(
					$"SELECT * FROM [UserSpendType] WITH (HOLDLOCK) WHERE [UserId] = {scope.OwnerId} AND [SpendTypeId] = {model.SpendTypeId}").AnyAsync()))
				throw new ServiceException("Spending category not found within the approved scope.", HttpStatusCode.NotFound);

			foreach (var field in modifyList)
			{
				switch (field)
				{
					case ClientEditScheduledTask.ScheduledTaskField.Amount:
						automaticTask.Amount = model.Amount;
						break;
					case ClientEditScheduledTask.ScheduledTaskField.SpendTypeId:
						automaticTask.SpendTypeId = model.SpendTypeId;
						break;
					case ClientEditScheduledTask.ScheduledTaskField.IsPending:
						automaticTask.IsPending = model.IsPending;
						break;
					case ClientEditScheduledTask.ScheduledTaskField.Description:
						automaticTask.TaskDescription = model.Description;
						break;
					case ClientEditScheduledTask.ScheduledTaskField.FrequencyType:
						automaticTask.PeriodTypeId = (int)model.FrequencyType;
						break;
					case ClientEditScheduledTask.ScheduledTaskField.Days:
						automaticTask.Days = ToStringCharSeparated(model.Days);
						break;
					case ClientEditScheduledTask.ScheduledTaskField.Invalid:
					default:
						throw new ArgumentException($"Invalid field {field}", nameof(model));
				}
			}

			MyFinanceBackend.Services.ScheduledExecutionRules.ValidateSchedule(
				(ScheduledTaskFrequencyType)automaticTask.PeriodTypeId, CreateArrayFromStringArray(automaticTask.Days));
			await Context.SaveChangesAsync();
			if (transaction != null)
				await transaction.CommitAsync();
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
					.Where(x => x.SpInTrxDefNavigation.RetiredAtUtc == null)
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
					.Where(x => x.TransferTrxDefNavigation.RetiredAtUtc == null)
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
				PeriodTypeId = (int)clientScheduledTask.FrequencyType,
				SpendTypeId = clientScheduledTask.SpendTypeId,
				UserId = userId,
				TaskDescription = clientScheduledTask.Description,
				IsPending = clientScheduledTask.IsPending
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
			scheduledTaskVm.IsPending = automaticTaskDef.AutomaticTaskNavigation.IsPending;
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
				if (int.TryParse(s, out result) && result >= 0)
				{
					list.Add(result);
				}
			}
			return list.ToArray();
		}
	}
}
