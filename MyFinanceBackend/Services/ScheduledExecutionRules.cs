using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public static class ScheduledExecutionRules
    {
        public static void Validate(ScheduledExecutionRequest request)
        {
            if (request == null || request.TaskId == Guid.Empty || request.ActorId == Guid.Empty ||
                request.OccurrenceId == Guid.Empty || request.ManualRequestId == Guid.Empty ||
                request.OccurrenceId.HasValue == request.ManualRequestId.HasValue ||
                (request.OccurrenceId.HasValue && request.ManualPostingDate.HasValue) ||
                (request.ManualRequestId.HasValue && !request.ManualPostingDate.HasValue))
                throw new ServiceException("Choose a scheduled occurrence or an independent manual request.", HttpStatusCode.BadRequest);
        }

        public static void ValidateSchedule(ScheduledTaskFrequencyType frequency, IEnumerable<int> days)
        {
            var values = days?.ToArray() ?? Array.Empty<int>();
            if (frequency == ScheduledTaskFrequencyType.Manual) return;
            var maximum = frequency == ScheduledTaskFrequencyType.Monthly ? 28 : 6;
            var minimum = frequency == ScheduledTaskFrequencyType.Monthly ? 1 : 0;
            if ((frequency != ScheduledTaskFrequencyType.Monthly && frequency != ScheduledTaskFrequencyType.Weekly) ||
                values.Length == 0 || values.Any(day => day < minimum || day > maximum))
                throw new ServiceException("Monthly days must be 1-28; weekly days must be 0-6.", HttpStatusCode.BadRequest);
        }
    }
}
