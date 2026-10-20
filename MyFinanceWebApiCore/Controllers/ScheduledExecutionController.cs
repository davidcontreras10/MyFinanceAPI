using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;

namespace MyFinanceWebApiCore.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ScheduledTasks")]
    public sealed class ScheduledExecutionController(IScheduledTaskExecutionService service,
        IScheduledTaskAuthorizationService authorizationService) : BaseApiController
    {
        private const int DefaultAttemptHistoryPageSize = 50;

        [HttpGet("{taskId:guid}/execution-attempts")]
        public async Task<ScheduledExecutionAttemptHistoryPage> GetAttemptHistoryAsync(Guid taskId,
            [FromQuery] int skip = 0, [FromQuery] int limit = DefaultAttemptHistoryPageSize,
            CancellationToken cancellationToken = default)
        {
            var permission = await authorizationService.AuthorizeAccessAsync(GetUserId(), taskId.ToString(), ResourceActionNames.View);
            if (permission?.IsAuthorized != true || permission.Scope == null)
                throw new ServiceException("Scheduled-task access is denied.", System.Net.HttpStatusCode.Forbidden);
            return await service.GetAttemptHistoryAsync(permission.Scope, skip, limit, cancellationToken);
        }

        [HttpPost("{taskId:guid}/execution-v2")]
        public async Task<ScheduledExecutionResult> ExecuteAsync(Guid taskId, ScheduledExecutionInput input,
            CancellationToken cancellationToken)
        {
            var actor = GetUserId();
            ArgumentNullException.ThrowIfNull(input);
            var permission = await authorizationService.AuthorizeExecutionAsync(actor, taskId.ToString(), input.OccurrenceId.HasValue);
            if (permission?.IsAuthorized != true || permission.Scope == null)
                throw new ServiceException("Task execution access is denied.", System.Net.HttpStatusCode.Forbidden);
            return await service.ExecuteAsync(new ScheduledExecutionRequest(permission.Scope.TaskId, permission.Scope.ActorId,
                input.OccurrenceId, input.ManualRequestId, input.PostingDate), permission.Scope, cancellationToken);
        }
    }
}
