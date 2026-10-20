using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public static class ScheduledExecutionRules
    {
        private const int MinimumMonthlyDay = 1;
        private const int MaximumMonthlyDay = 28;
        private const int MinimumWeeklyDay = 0;
        private const int MaximumWeeklyDay = 6;

        public static void ValidateCreation(ClientScheduledTask request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ValidateSchedule(request.FrequencyType, request.Days);
            if (request is not (ClientScheduledTask.Basic or ClientScheduledTask.Transfer) ||
                request.AccountId <= 0 || request.SpendTypeId <= 0 || request.CurrencyId <= 0 ||
                request is ClientScheduledTask.Transfer { ToAccountId: <= 0 } ||
                !float.IsFinite(request.Amount) || request.Amount <= 0 || string.IsNullOrWhiteSpace(request.Description))
                throw new ServiceException("A valid task definition is required.", HttpStatusCode.BadRequest);
        }

        public static void Validate(ScheduledExecutionRequest request)
        {
            if (request == null || request.TaskId == Guid.Empty || request.ActorId == Guid.Empty || request.ExpectedOwnerId == Guid.Empty ||
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
            var maximum = frequency == ScheduledTaskFrequencyType.Monthly ? MaximumMonthlyDay : MaximumWeeklyDay;
            var minimum = frequency == ScheduledTaskFrequencyType.Monthly ? MinimumMonthlyDay : MinimumWeeklyDay;
            if ((frequency != ScheduledTaskFrequencyType.Monthly && frequency != ScheduledTaskFrequencyType.Weekly) ||
                values.Length == 0 || values.Any(day => day < minimum || day > maximum))
                throw new ServiceException($"Monthly days must be {MinimumMonthlyDay}-{MaximumMonthlyDay}; weekly days must be {MinimumWeeklyDay}-{MaximumWeeklyDay}.", HttpStatusCode.BadRequest);
        }
    }
}
