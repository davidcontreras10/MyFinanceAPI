using MyFinanceModel.ViewModel;

namespace MyFinanceModel
{
	public class TaskExecutedResult
	{
		private TaskExecutedResult(string taskId)
		{
			TaskId = taskId;
		}

		public string TaskId { get; }
		public ExecutedTaskStatus Status { get; set; }
		public string ErrorMsg { get; set; }

		public static TaskExecutedResult Error(string errorMsg, string taskId)
		{
			return new TaskExecutedResult(taskId)
			{
				ErrorMsg = errorMsg,
				Status = ExecutedTaskStatus.Failed
			};
		}

		public static TaskExecutedResult Success(string taskId)
		{
			return new TaskExecutedResult(taskId)
			{
				Status = ExecutedTaskStatus.Succeeded
			};
		}
	}
}
