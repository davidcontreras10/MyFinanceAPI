using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFinanceBackend.Services;
using MyFinanceModel;

namespace MyFinanceWebApiCore.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/ScheduledTasks")]
    public sealed class ScheduledExecutionController(IScheduledTaskExecutionService service) : BaseApiController
    {
        [HttpPost("{taskId:guid}/execution-v2")]
        public Task<ScheduledExecutionResult> ExecuteAsync(Guid taskId, ScheduledExecutionInput input,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(GetUserId(), out var actorId) || actorId == Guid.Empty)
                throw new UnauthorizedAccessException();
            ArgumentNullException.ThrowIfNull(input);
            return service.ExecuteAsync(new ScheduledExecutionRequest(taskId, actorId,
                input.OccurrenceId, input.ManualRequestId, input.PostingDate), cancellationToken);
        }
    }
}
