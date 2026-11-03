using System;
using System.Collections.Generic;

namespace ApiFunctions.Models
{
	public sealed class ScheduledTaskDispatchFailure
	{
		public Guid TaskId { get; set; }
		public string Stage { get; set; }
		public string Error { get; set; }
	}

	public sealed class ScheduledTaskDispatchResult
	{
		public int SchedulesScanned { get; set; }
		public int OccurrencesAdded { get; set; }
		public int ExecutionsCompleted { get; set; }
		public int AlreadyCompleted { get; set; }
		public int FailedCount { get; set; }
		public bool HasMoreDiscovery { get; set; }
		public bool HasMoreDueWork { get; set; }
		public IReadOnlyList<ScheduledTaskDispatchFailure> Failures { get; set; } = Array.Empty<ScheduledTaskDispatchFailure>();
	}
}
