using System;
using System.Net;
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
    public sealed class ScheduledTaskDispatchController(IScheduledTaskDispatcher dispatcher) : BaseApiController
    {
        [HttpPost("dispatch-v2")]
        public Task<ScheduledTaskDispatchResult> DispatchAsync(CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(GetUserId(), out var actorId) || actorId == Guid.Empty)
                throw new ServiceException("An authenticated administrator is required.", HttpStatusCode.Unauthorized);
            return dispatcher.DispatchDueAsync(actorId, cancellationToken);
        }
    }
}
