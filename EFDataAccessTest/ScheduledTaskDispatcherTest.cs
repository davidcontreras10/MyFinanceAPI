using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Data;
using MyFinanceBackend.Services;
using MyFinanceModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    [TestFixture]
    public sealed class ScheduledTaskDispatcherTest
    {
        private static readonly Guid AdminId = Guid.NewGuid();

        [Test]
        public void DispatcherIsOffUnlessExplicitlyEnabled()
        {
            var schedules = new ScheduleRepository();
            var dispatcher = CreateDispatcher(schedules, new ExecutionService(), dispatcherEnabled: false);

            var error = Assert.ThrowsAsync<ServiceException>(() => dispatcher.DispatchDueAsync(AdminId));

            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(schedules.GetSchedulesCalled, Is.False);
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        public void DispatcherAlsoRequiresHistoryAndExecutionFlags(bool historyEnabled, bool executionEnabled)
        {
            var schedules = new ScheduleRepository();
            var dispatcher = CreateDispatcher(schedules, new ExecutionService(), dispatcherEnabled: true,
                historyEnabled: historyEnabled, executionEnabled: executionEnabled);

            var error = Assert.ThrowsAsync<ServiceException>(() => dispatcher.DispatchDueAsync(AdminId));

            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));
            Assert.That(schedules.GetSchedulesCalled, Is.False);
        }

        [Test]
        public async Task DispatchUsesBoundedBatchesAndExecutesPendingWorkWithAdminScope()
        {
            var taskId = Guid.NewGuid();
            var ownerId = Guid.NewGuid();
            var occurrenceId = Guid.NewGuid();
            var schedules = new ScheduleRepository
            {
                ScheduleIds = new[] { taskId },
                Pending = new[] { new PendingScheduledOccurrence(occurrenceId, taskId, ownerId,
                    new DateTime(2026, 10, 16), new DateTime(2026, 10, 16, 6, 0, 0, DateTimeKind.Utc)) },
                Discovery = _ => new ScheduledDiscoveryResult(2, DateTime.UtcNow, false)
            };
            var executions = new ExecutionService();
            var dispatcher = CreateDispatcher(schedules, executions, dispatcherEnabled: true);

            var result = await dispatcher.DispatchDueAsync(AdminId);

            Assert.That(schedules.ScanLimit, Is.EqualTo(50));
            Assert.That(schedules.PendingLimit, Is.EqualTo(50));
            Assert.That(result.SchedulesScanned, Is.EqualTo(1));
            Assert.That(result.OccurrencesAdded, Is.EqualTo(2));
            Assert.That(result.ExecutionsCompleted, Is.EqualTo(1));
            Assert.That(result.AlreadyCompleted, Is.Zero);
            Assert.That(result.FailedCount, Is.Zero);
            Assert.That(executions.Requests.Single().OccurrenceId, Is.EqualTo(occurrenceId));
            Assert.That(executions.Scopes.Single().ActorId, Is.EqualTo(AdminId));
            Assert.That(executions.Scopes.Single().OwnerId, Is.EqualTo(ownerId));
            Assert.That(executions.Scopes.Single().AdministratorDispatch, Is.True);
        }

        [Test]
        public async Task DiscoveryAndExecutionFailuresDoNotBlockOtherTasks()
        {
            var firstTask = Guid.NewGuid();
            var secondTask = Guid.NewGuid();
            var schedules = new ScheduleRepository
            {
                ScheduleIds = new[] { firstTask, secondTask },
                Discovery = taskId => taskId == firstTask
                    ? throw new InvalidOperationException("discovery failed")
                    : new ScheduledDiscoveryResult(1, DateTime.UtcNow, false),
                Pending = new[]
                {
                    new PendingScheduledOccurrence(Guid.NewGuid(), firstTask, Guid.NewGuid(), DateTime.Today, DateTime.UtcNow),
                    new PendingScheduledOccurrence(Guid.NewGuid(), secondTask, Guid.NewGuid(), DateTime.Today, DateTime.UtcNow)
                }
            };
            var executions = new ExecutionService
            {
                Execute = request => request.TaskId == firstTask
                    ? throw new InvalidOperationException("execution failed")
                    : new ScheduledExecutionResult(Guid.NewGuid(), false)
            };
            var dispatcher = CreateDispatcher(schedules, executions, dispatcherEnabled: true);

            var result = await dispatcher.DispatchDueAsync(AdminId);

            Assert.That(result.ExecutionsCompleted, Is.EqualTo(1));
            Assert.That(result.FailedCount, Is.EqualTo(2));
            Assert.That(result.Failures.Select(x => x.Stage), Is.EquivalentTo(new[] { "discovery", "execution" }));
            Assert.That(executions.Requests.Select(x => x.TaskId), Does.Contain(secondTask));
        }

        private static IScheduledTaskDispatcher CreateDispatcher(ScheduleRepository schedules,
            ExecutionService executions, bool dispatcherEnabled, bool historyEnabled = true, bool executionEnabled = true) =>
            new ScheduledTaskDispatcher(schedules, executions,
                new ScheduledExecutionOptions { Enabled = executionEnabled, HistoryEnabled = historyEnabled },
                new ScheduledTaskDispatchOptions { Enabled = dispatcherEnabled },
                NullLogger<ScheduledTaskDispatcher>.Instance);

        private sealed class ScheduleRepository : IScheduledScheduleRepository
        {
            public IReadOnlyList<Guid> ScheduleIds { get; init; } = Array.Empty<Guid>();
            public IReadOnlyList<PendingScheduledOccurrence> Pending { get; init; } = Array.Empty<PendingScheduledOccurrence>();
            public Func<Guid, ScheduledDiscoveryResult> Discovery { get; init; } = _ => new(0, DateTime.UtcNow, false);
            public bool GetSchedulesCalled { get; private set; }
            public int ScanLimit { get; private set; }
            public int PendingLimit { get; private set; }

            public Task<IReadOnlyList<Guid>> GetSchedulesToScanAsync(Guid administratorId, int limit,
                CancellationToken cancellationToken = default)
            {
                GetSchedulesCalled = true;
                ScanLimit = limit;
                return Task.FromResult(ScheduleIds);
            }

            public Task<ScheduledDiscoveryResult> DiscoverAsync(Guid taskId, Guid actorId,
                CancellationToken cancellationToken = default) => Task.FromResult(Discovery(taskId));

            public Task<IReadOnlyList<PendingScheduledOccurrence>> GetPendingAsync(Guid actorId, int limit,
                CancellationToken cancellationToken = default)
            {
                PendingLimit = limit;
                return Task.FromResult(Pending);
            }

            public Task<ScheduledTaskExecutionContext> GetExecutionContextAsync(ScheduledTaskAccessScope scope,
                CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<ScheduledTaskHistoryPage> GetExecutionHistoryAsync(ScheduledTaskAccessScope scope, int skip, int limit,
                CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task RetireAsync(ScheduledTaskAccessScope scope, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<TaskExecutedResult> ExecuteLegacyAsync(ScheduledTaskExecutionScope scope,
                Func<Task<TaskExecutedResult>> execute, CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task<Guid> RegisterAsync(Guid ownerId, Func<Task<Guid>> createTask,
                CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task ActivateAsync(Guid taskId, Guid administratorId, DateTimeOffset activationUtc, string timeZoneId,
                CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public Task ReviseAsync(Guid taskId, Guid ownerId, Func<Task> update, string timeZoneId = null,
                CancellationToken cancellationToken = default) => throw new NotImplementedException();
        }

        private sealed class ExecutionService : IScheduledTaskExecutionService
        {
            public List<ScheduledExecutionRequest> Requests { get; } = new();
            public List<ScheduledTaskExecutionScope> Scopes { get; } = new();
            public Func<ScheduledExecutionRequest, ScheduledExecutionResult> Execute { get; init; } =
                _ => new ScheduledExecutionResult(Guid.NewGuid(), false);

            public Task<ScheduledExecutionResult> ExecuteAsync(ScheduledExecutionRequest request,
                ScheduledTaskExecutionScope scope, CancellationToken cancellationToken = default)
            {
                Requests.Add(request);
                Scopes.Add(scope);
                return Task.FromResult(Execute(request));
            }
        }
    }
}
