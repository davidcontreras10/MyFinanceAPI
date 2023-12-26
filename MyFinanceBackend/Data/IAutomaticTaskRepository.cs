using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MyFinanceBackend.Data
{
	public interface IAutomaticTaskRepository : ITransactional
	{
		Task InsertBasicTrxAsync(
			string userId,
			ClientScheduledTask.Basic clientScheduledTask
		);

		Task InsertTransferTrxAsync(
			string userId,
			ClientScheduledTask.Transfer clientScheduledTask
		);

		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByUserIdAsync(string userId);
		Task<IReadOnlyCollection<ExecutedTaskViewModel>> GetExecutedTasksByTaskIdAsync(string taskId);
		Task DeleteByIdAsync(string taskId);
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledByTaskIdAsync(string taskId);
		Task RecordClientExecutedTaskAsync(ClientExecutedTask clientExecutedTask);
		Task<IReadOnlyCollection<BaseScheduledTaskVm>> GetScheduledTasksAsync();
	}
}
