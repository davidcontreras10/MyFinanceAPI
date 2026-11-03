using System.Security.Cryptography;
using System.Text;
using EFDataAccess.Models;
using EFDataAccess.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Services;

namespace ScheduledTaskActivation;

public sealed record ActivationPlan
{
    public string ExpectedServer { get; init; } = "";
    public string ExpectedDatabase { get; init; } = "";
    public Guid AdministratorId { get; init; }
    public DateTimeOffset ActivationUtc { get; init; }
    public bool Reviewed { get; init; }
    public string ApprovalNote { get; init; } = "";
    public IReadOnlyList<ActivationTask> Tasks { get; init; } = Array.Empty<ActivationTask>();
}

public sealed record ActivationTask
{
    public Guid TaskId { get; init; }
    public Guid OwnerId { get; init; }
    public string? TimeZoneId { get; init; }
    public string? DefinitionSha256 { get; init; }
    public string? Description { get; init; }
    public DateTime? FirstEligibleLocalDate { get; init; }
}

public sealed class ActivationRunner(string connection, TimeProvider clock)
{
    private const int MaximumTaskCount = 100;

    public static void ValidateSelection(ActivationPlan plan, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (string.IsNullOrWhiteSpace(plan.ExpectedServer) || string.IsNullOrWhiteSpace(plan.ExpectedDatabase))
            throw new ArgumentException("An explicit expected server and database are required.");
        if (plan.AdministratorId == Guid.Empty || plan.ActivationUtc == default ||
            plan.ActivationUtc.Offset != TimeSpan.Zero || plan.ActivationUtc > now)
            throw new ArgumentException("A persisted administrator and a nonfuture UTC activation boundary are required.");
        if (plan.Tasks == null || plan.Tasks.Count == 0 || plan.Tasks.Count > MaximumTaskCount ||
            plan.Tasks.Any(x => x == null || x.TaskId == Guid.Empty || x.OwnerId == Guid.Empty) ||
            plan.Tasks.Select(x => x.TaskId).Distinct().Count() != plan.Tasks.Count)
            throw new ArgumentException($"Supply 1-{MaximumTaskCount} unique task IDs with their expected owners.");
    }

    public static void ValidateTarget(ActivationPlan plan, string connectionString)
    {
        var target = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(target.DataSource, plan.ExpectedServer, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(target.InitialCatalog, plan.ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The connection does not match the reviewed server and database.");
    }

    public Task<ActivationPlan> PreviewAsync(ActivationPlan plan, CancellationToken cancellationToken = default) =>
        ReadPlanAsync(plan, requireSnapshots: false, cancellationToken);

    public async Task<IReadOnlyList<Guid>> ApplyAsync(ActivationPlan plan, Action<Guid>? confirmed = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSelection(plan, clock.GetUtcNow());
        if (!plan.Reviewed || string.IsNullOrWhiteSpace(plan.ApprovalNote))
            throw new ArgumentException("Apply requires reviewed=true and an approval note confirming cutoff reconciliation and maintenance.");
        // Preflight the entire allowlist before committing any task. Per-task repository
        // transactions are independently resumable; this is deliberately not a batch transaction.
        await ReadPlanAsync(plan, requireSnapshots: true, cancellationToken);
        var completed = new List<Guid>();
        foreach (var task in plan.Tasks.OrderBy(x => x.OwnerId).ThenBy(x => x.TaskId))
        {
            await ReadPlanAsync(plan with { Tasks = new[] { task } }, requireSnapshots: true, cancellationToken);
            await using var context = Context();
            var schedules = new EFScheduledScheduleRepository(context, clock,
                new EFAccountRepository(context, NullLogger<EFAccountRepository>.Instance),
                new ScheduledExecutionOptions { HistoryEnabled = true });
            await schedules.ActivateAsync(task.TaskId, plan.AdministratorId, plan.ActivationUtc, task.TimeZoneId!, cancellationToken);
            completed.Add(task.TaskId);
            confirmed?.Invoke(task.TaskId);
        }
        return completed;
    }

    private async Task<ActivationPlan> ReadPlanAsync(ActivationPlan plan, bool requireSnapshots,
        CancellationToken cancellationToken)
    {
        ValidateSelection(plan, clock.GetUtcNow());
        ValidateTarget(plan, connection);
        await using var context = Context();
        var definitions = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
        if (!await definitions.IsAdministratorAsync(plan.AdministratorId))
            throw new ArgumentException("The administrator must have a persisted Admin role in the target database.");
        var tasks = new List<ActivationTask>();
        foreach (var selected in plan.Tasks)
        {
            var task = await context.AutomaticTask.AsNoTracking().Include(x => x.SpInTrxDef).Include(x => x.TransferTrxDef).SingleOrDefaultAsync(
                x => x.AutomaticTaskId == selected.TaskId, cancellationToken);
            if (task == null || task.UserId != selected.OwnerId || task.RetiredAtUtc.HasValue)
                throw new ArgumentException($"Task {selected.TaskId:D} is missing, retired or not owned by the expected user.");
            var zone = await context.AppUser.AsNoTracking().Where(x => x.UserId == selected.OwnerId)
                .Select(x => x.SchedulingTimeZoneId).SingleAsync(cancellationToken);
            ScheduledOccurrenceCalendar.ResolveTimeZone(zone);
            var definition = ScheduledTaskSnapshots.Create(task);
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{plan.ExpectedServer}\n{plan.ExpectedDatabase}\n{plan.AdministratorId:D}\n{plan.ActivationUtc:O}\n" +
                $"{task.UserId:D}\n{zone}\n{ScheduledTaskDefinitionCodec.Write(definition)}")));
            if (requireSnapshots && (selected.TimeZoneId != zone ||
                !string.Equals(selected.DefinitionSha256, hash, StringComparison.Ordinal)))
                throw new ArgumentException($"Task {selected.TaskId:D} has no matching reviewed snapshot. Preview and review again.");
            var state = await context.Set<ScheduledTaskSchedule>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.TaskId == selected.TaskId, cancellationToken);
            if (state != null)
            {
                var first = await context.Set<ScheduledTaskRevision>().AsNoTracking().Where(x => x.TaskId == selected.TaskId)
                    .OrderBy(x => x.EffectiveFromUtc).FirstAsync(cancellationToken);
                if (state.OwnerId != selected.OwnerId || state.ActivatedAtUtc != plan.ActivationUtc.UtcDateTime || first.TimeZoneId != zone)
                    throw new ArgumentException($"Task {selected.TaskId:D} already has a different activation boundary or initial timezone; it cannot be reset.");
            }
            var due = ScheduledOccurrenceCalendar.FindDueDates(definition, zone, plan.ActivationUtc.UtcDateTime,
                plan.ActivationUtc.UtcDateTime.AddDays(ScheduledOccurrenceCalendar.ScanWindowDays));
            tasks.Add(selected with { TimeZoneId = zone, DefinitionSha256 = hash,
                Description = definition.Description, FirstEligibleLocalDate = due.FirstOrDefault()?.LocalDate });
        }
        return plan with { Reviewed = false, Tasks = tasks };
    }

    private MyFinanceContext Context() => new(new DbContextOptionsBuilder<MyFinanceContext>().UseSqlServer(connection).Options);
}
