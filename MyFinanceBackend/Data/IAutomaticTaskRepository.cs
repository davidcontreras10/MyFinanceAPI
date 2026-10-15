using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using MyFinanceModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IAutomaticTaskRepository : ITransactional
	{
		Task<Guid> CreateAsync(ScheduledTaskCreateScope scope, ClientScheduledTask model);
		Task InsertBasicTrxAsync(
			string userId,
			ClientScheduledTask.Basic clientScheduledTask
		);

		Task InsertTransferTrxAsync(
			string userId,
			ClientScheduledTask.Transfer clientScheduledTask
		);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByUserIdAsync(string userId);
		Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(ScheduledTaskAccessScope scope);
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByTaskIdAsync(string taskId);
		Task RecordClientExecutedTaskAsync(ClientExecutedTask clientExecutedTask);
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync();
		Task EditScheduledTaskAsync(ClientEditScheduledTask model, ScheduledTaskEditScope scope);
	}
}
