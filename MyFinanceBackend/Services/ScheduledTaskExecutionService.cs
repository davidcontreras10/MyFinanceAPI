using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MyFinanceBackend.Data;
using MyFinanceModel;
using MyFinanceModel.ViewModel;
using Newtonsoft.Json;

namespace MyFinanceBackend.Services
{
    public sealed class ScheduledExecutionOptions
    {
        public bool Enabled { get; init; }
    }

    public interface IScheduledTaskExecutionService
    {
        Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class ScheduledTaskExecutionService(IScheduledExecutionRepository repository,
        IScheduledTasksService financialExecutor, IAccountAccessRepository accountAccess,
        ScheduledExecutionOptions options) : IScheduledTaskExecutionService
    {
        public Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!options.Enabled)
                throw new ServiceException("The new scheduled execution path is not enabled.", HttpStatusCode.ServiceUnavailable);
            ScheduledExecutionRules.Validate(request);
            return repository.ExecuteAsync(request, ExecuteFinancialWorkAsync, cancellationToken);
        }

        private async Task ExecuteFinancialWorkAsync(ScheduledExecutionWork work, CancellationToken cancellationToken)
        {
            BaseScheduledTaskVm definition;
            try { definition = JsonConvert.DeserializeObject<BaseScheduledTaskVm>(work.DefinitionJson); }
            catch (JsonException)
            {
                throw new ServiceException("The stored task definition is invalid.", HttpStatusCode.Conflict);
            }
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
