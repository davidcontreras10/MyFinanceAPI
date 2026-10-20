using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EFDataAccess.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFinanceModel.ViewModel;
using NUnit.Framework;
using ScheduledTaskActivation;

namespace EFDataAccessTest
{
    public sealed partial class ScheduledFinancialWorkflowSqlTest
    {
        private const string ActivationTestApproval = "Local copy rehearsal: temporary tasks only; cutoff reviewed and edits paused.";

        [Test]
        public async Task ActivationPreviewIsReadOnlyAndReviewedResumePreservesTheCutoff()
        {
            var marker = "Activation rehearsal " + Guid.NewGuid().ToString("N");
            var tasks = new List<Guid>();
            var accounts = new List<int>();
            var administratorId = Guid.NewGuid();
            var clock = new FinancialDispatchClock(DispatchInstant(DispatchFirstDate.AddDays(2)));
            try
            {
                PostingReferences posting;
                Guid selected;
                Guid excluded;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await CreateDispatchReferencesAsync(setup, marker, administratorId, accounts);
                    selected = await CreateFinancialTaskAsync(setup, posting, false, marker + " selected", daily: true);
                    tasks.Add(selected);
                    excluded = await CreateFinancialTaskAsync(setup, posting, false, marker + " excluded", daily: true);
                    tasks.Add(excluded);
                }
                var runner = new ActivationRunner(_connection, clock);
                var selection = ActivationSelection(administratorId, posting.OwnerId, selected);
                var preview = await PreviewViaCommandAsync(selection);
                Assert.That(preview.Reviewed, Is.False);
                Assert.That(preview.Tasks.Single().DefinitionSha256, Has.Length.EqualTo(64));
                Assert.That(preview.Tasks.Single().FirstEligibleLocalDate, Is.Not.Null);
                await using (var verification = new MyFinanceContext(Options()))
                {
                    Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskRevision>().AnyAsync(), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskOccurrence>().AnyAsync(), Is.False);
                }
                var approved = preview with { Reviewed = true, ApprovalNote = ActivationTestApproval };
                Assert.That(await runner.ApplyAsync(approved), Is.EqualTo(new[] { selected }));
                DateTime cursor;
                DateTime[] dates;
                await using (var verification = new MyFinanceContext(Options()))
                {
                    var state = await verification.Set<ScheduledTaskSchedule>().SingleAsync();
                    Assert.That(state.TaskId, Is.EqualTo(selected));
                    Assert.That(state.ActivatedAtUtc, Is.EqualTo(approved.ActivationUtc.UtcDateTime));
                    await DispatchSchedules(verification, clock).DiscoverAsync(selected, administratorId);
                    dates = await verification.Set<ScheduledTaskOccurrence>().OrderBy(x => x.ScheduledLocalDate)
                        .Select(x => x.ScheduledLocalDate).ToArrayAsync();
                    Assert.That(dates.First(), Is.EqualTo(preview.Tasks.Single().FirstEligibleLocalDate));
                    Assert.That(await verification.Set<ScheduledTaskOccurrence>().AnyAsync(x => x.DueAtUtc < approved.ActivationUtc.UtcDateTime), Is.False);
                    cursor = (await verification.Set<ScheduledTaskSchedule>().AsNoTracking().SingleAsync()).NextScanUtc;
                }
                Assert.That(await runner.ApplyAsync(approved), Is.EqualTo(new[] { selected }));
                await using (var verification = new MyFinanceContext(Options()))
                {
                    Assert.That((await verification.Set<ScheduledTaskSchedule>().SingleAsync()).NextScanUtc, Is.EqualTo(cursor));
                    Assert.That(await verification.Set<ScheduledTaskRevision>().CountAsync(), Is.EqualTo(1));
                    Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(x => x.TaskId == excluded), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskOccurrence>().OrderBy(x => x.ScheduledLocalDate)
                        .Select(x => x.ScheduledLocalDate).ToArrayAsync(), Is.EqualTo(dates));
                    Assert.That(await verification.Set<ScheduledTaskCompletion>().AnyAsync(), Is.False);
                    Assert.That(await verification.Set<ScheduledTaskExecutionAttempt>().AnyAsync(), Is.False);
                    Assert.That(await verification.ExecutedTask.AnyAsync(x => tasks.Contains(x.AutomaticTaskId)), Is.False);
                    Assert.That(await verification.Spend.AnyAsync(x => x.Description.Contains(marker)), Is.False);
                }
                Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(approved with { ActivationUtc = approved.ActivationUtc.AddDays(-1) }));
            }
            finally
            {
                await CleanupAsync(marker, tasks);
                await CleanupDispatchReferencesAsync(accounts, administratorId);
            }
        }

        [Test]
        public async Task ActivationRequiresReviewAndPreflightsAllSnapshotsBeforeWriting()
        {
            var marker = "Activation rejection test " + Guid.NewGuid().ToString("N");
            var tasks = new List<Guid>();
            var accounts = new List<int>();
            var administratorId = Guid.NewGuid();
            var clock = new FinancialDispatchClock(DispatchInstant(DispatchFirstDate.AddDays(2)));
            try
            {
                PostingReferences posting;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await CreateDispatchReferencesAsync(setup, marker, administratorId, accounts);
                    tasks.Add(await CreateFinancialTaskAsync(setup, posting, false, marker + " first", daily: true));
                    tasks.Add(await CreateFinancialTaskAsync(setup, posting, true, marker + " second", daily: true));
                }
                var runner = new ActivationRunner(_connection, clock);
                var selection = ActivationSelection(administratorId, posting.OwnerId, tasks.ToArray());
                var preview = await runner.PreviewAsync(selection);
                Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(preview));
                Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(preview with
                    { Reviewed = true, ApprovalNote = ActivationTestApproval, ActivationUtc = preview.ActivationUtc.AddDays(-1) }));
                Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(selection with { Reviewed = true, ApprovalNote = ActivationTestApproval }));
                Assert.ThrowsAsync<ArgumentException>(() => runner.PreviewAsync(selection with { AdministratorId = Guid.NewGuid() }));
                Assert.ThrowsAsync<ArgumentException>(() => runner.PreviewAsync(selection with
                    { Tasks = new[] { selection.Tasks[0] with { OwnerId = Guid.NewGuid() } } }));
                await using (var changed = new MyFinanceContext(Options()))
                {
                    var task = await changed.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == tasks[1]);
                    task.TaskDescription += " edited after preview";
                    await changed.SaveChangesAsync();
                }
                Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(preview with { Reviewed = true, ApprovalNote = ActivationTestApproval }));
                await using var verification = new MyFinanceContext(Options());
                Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(), Is.False);
                Assert.That(await verification.Set<ScheduledTaskRevision>().AnyAsync(), Is.False);
                Assert.That(await verification.Set<ScheduledTaskOccurrence>().AnyAsync(), Is.False);
            }
            finally
            {
                await CleanupAsync(marker, tasks);
                await CleanupDispatchReferencesAsync(accounts, administratorId);
            }
        }

        [Test]
        public async Task ActivationPreviewRejectsLegacyMonthly29WithoutChangingTheSchedule()
        {
            var marker = "Activation invalid month test " + Guid.NewGuid().ToString("N");
            var tasks = new List<Guid>();
            var accounts = new List<int>();
            var administratorId = Guid.NewGuid();
            var clock = new FinancialDispatchClock(DispatchInstant(DispatchFirstDate.AddDays(2)));
            try
            {
                PostingReferences posting;
                await using (var setup = new MyFinanceContext(Options()))
                {
                    posting = await CreateDispatchReferencesAsync(setup, marker, administratorId, accounts);
                    var id = await CreateFinancialTaskAsync(setup, posting, false, marker, daily: true);
                    tasks.Add(id);
                    var task = await setup.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == id);
                    task.PeriodTypeId = (int)ScheduledTaskFrequencyType.Monthly;
                    task.Days = "29";
                    await setup.SaveChangesAsync();
                }
                var runner = new ActivationRunner(_connection, clock);
                Assert.ThrowsAsync<MyFinanceModel.ServiceException>(() => runner.PreviewAsync(
                    ActivationSelection(administratorId, posting.OwnerId, tasks.ToArray())));
                await using var verification = new MyFinanceContext(Options());
                Assert.That((await verification.AutomaticTask.SingleAsync(x => x.AutomaticTaskId == tasks[0])).Days, Is.EqualTo("29"));
                Assert.That(await verification.Set<ScheduledTaskSchedule>().AnyAsync(), Is.False);
            }
            finally
            {
                await CleanupAsync(marker, tasks);
                await CleanupDispatchReferencesAsync(accounts, administratorId);
            }
        }

        private async Task<ActivationPlan> PreviewViaCommandAsync(ActivationPlan plan)
        {
            const string connectionVariable = "MYFINANCE_SCHEDULE_ACTIVATION_CONNECTION";
            var previousConnection = Environment.GetEnvironmentVariable(connectionVariable);
            var previousOutput = Console.Out;
            var path = Path.Combine(AppContext.BaseDirectory, "activation-preview-" + Guid.NewGuid().ToString("N") + ".json");
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            using var output = new StringWriter();
            try
            {
                await File.WriteAllTextAsync(path, JsonSerializer.Serialize(plan, jsonOptions), new UTF8Encoding(true));
                Environment.SetEnvironmentVariable(connectionVariable, _connection);
                Console.SetOut(output);
                Assert.That(await ScheduledTaskActivation.Program.Main(new[] { "--plan", path }), Is.Zero);
                return JsonSerializer.Deserialize<ActivationPlan>(output.ToString(), jsonOptions);
            }
            finally
            {
                Console.SetOut(previousOutput);
                Environment.SetEnvironmentVariable(connectionVariable, previousConnection);
                File.Delete(path);
            }
        }

        private ActivationPlan ActivationSelection(Guid administratorId, Guid ownerId, params Guid[] taskIds)
        {
            var target = new SqlConnectionStringBuilder(_connection);
            return new ActivationPlan
            {
                ExpectedServer = target.DataSource, ExpectedDatabase = target.InitialCatalog,
                AdministratorId = administratorId, ActivationUtc = DispatchInstant(DispatchFirstDate.AddDays(-1)),
                Tasks = taskIds.Select(id => new ActivationTask { TaskId = id, OwnerId = ownerId }).ToArray()
            };
        }
    }
}
