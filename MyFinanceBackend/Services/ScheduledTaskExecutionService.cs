using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace MyFinanceBackend.Services
{
    public sealed class ScheduledExecutionOptions
    {
        public bool Enabled { get; init; }
        public bool HistoryEnabled { get; init; }
    }

    public interface IScheduledTaskExecutionService
    {
        Task<ScheduledExecutionAttemptHistoryPage> GetAttemptHistoryAsync(ScheduledTaskAccessScope scope,
            int skip, int limit, CancellationToken cancellationToken = default);

        Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request, ScheduledTaskExecutionScope scope,
            CancellationToken cancellationToken = default);
    }

    public sealed class ScheduledTaskExecutionService(IScheduledExecutionRepository repository,
        IScheduledTasksService financialExecutor, IAccountAccessRepository accountAccess,
        ScheduledExecutionOptions options) : IScheduledTaskExecutionService
    {
        public Task<ScheduledExecutionAttemptHistoryPage> GetAttemptHistoryAsync(ScheduledTaskAccessScope scope,
            int skip, int limit, CancellationToken cancellationToken = default)
        {
            if (!options.HistoryEnabled)
                throw new ServiceException("Scheduled execution history is not enabled.", HttpStatusCode.ServiceUnavailable);
            ArgumentNullException.ThrowIfNull(scope);
            if (scope.Action != ResourceActionNames.View)
                throw new ServiceException("The approved scope does not permit viewing execution history.", HttpStatusCode.Forbidden);
            return repository.GetAttemptHistoryAsync(scope, skip, limit, cancellationToken);
        }

        public Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request, ScheduledTaskExecutionScope scope,
            CancellationToken cancellationToken = default)
        {
            if (!options.Enabled)
                throw new ServiceException("The new scheduled execution path is not enabled.", HttpStatusCode.ServiceUnavailable);
            ScheduledExecutionRules.Validate(request);
            ArgumentNullException.ThrowIfNull(scope);
            if (request.TaskId != scope.TaskId || request.ActorId != scope.ActorId ||
                (!request.OccurrenceId.HasValue && scope.ActorId != scope.OwnerId))
                throw new ServiceException("The execution does not match the approved scope.", HttpStatusCode.Forbidden);
            return repository.ExecuteAsync(request with { ExpectedOwnerId = scope.OwnerId }, ExecuteFinancialWorkAsync, cancellationToken);
        }

        private async Task ExecuteFinancialWorkAsync(ScheduledExecutionWork work, CancellationToken cancellationToken)
        {
            var definition = ScheduledTaskDefinitionCodec.Read(work.DefinitionJson);
            if (definition == null || definition.Id != work.TaskId || definition.AccountId <= 0 ||
                definition.SpendTypeId <= 0 || definition.CurrencyId <= 0 || !float.IsFinite(definition.Amount) || definition.Amount <= 0 ||
                (definition is TransferScheduledTaskVm transfer && transfer.ToAccountId <= 0))
                throw new ServiceException("The stored task definition is invalid.", HttpStatusCode.Conflict);

            var accountIds = definition is TransferScheduledTaskVm target
                ? new[] { definition.AccountId, target.ToAccountId } : new[] { definition.AccountId };
            if (!await accountAccess.IsWithinOwnerScopeAsync(work.OwnerId, new AccountAccessTargets
                { AccountIds = accountIds, SpendTypeIds = new[] { definition.SpendTypeId } }))
                throw new ServiceException("Task accounts or category are outside the owner's scope.", HttpStatusCode.Forbidden);

            cancellationToken.ThrowIfCancellationRequested();
            var result = await financialExecutor.ExecuteDefinitionAsync(definition, work.PostingDate,
                work.OccurrenceId.HasValue ? ExecuteTaskRequestType.Automatic : ExecuteTaskRequestType.Manual,
                work.ActorId.ToString());
            if (result.Status != ExecutedTaskStatus.Succeeded)
                throw new ServiceException("Task execution failed; retry the same execution identity after resolving the failure.", HttpStatusCode.Conflict);
        }
    }
}
