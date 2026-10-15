using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using EFDataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyFinanceBackend.Services;
using MyFinanceBackend.Services.AuthServices;
using MyFinanceModel;
using MyFinanceModel.ClientViewModel;
using MyFinanceModel.ViewModel;
using NUnit.Framework;

namespace EFDataAccessTest
{
    public partial class ScheduledExecutionSqlTest
    {
        private static ClientEditScheduledTask DescriptionEdit(Guid bodyTaskId) => new()
        {
            TaskId = bodyTaskId.ToString(), Description = "Edited within scope",
            ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.Description }
        };

        [TestCase(false)]
        [TestCase(true)]
        public async Task EditUsesApprovedTargetAndOwnerWithEitherHistorySetting(bool historyEnabled)
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            var otherTask = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 15);
            await using (var context = Context())
            {
                var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
                Assert.That(await repository.IsOwnedByAsync(task, _owner), Is.True);
                Assert.That(await repository.IsOwnedByAsync(task, _other), Is.False);
                var service = new ScheduledTasksService(repository, null, null, null, null,
                    Schedules(context, new ScheduleClock(Instant(1, 1))), new ScheduledExecutionOptions { HistoryEnabled = historyEnabled });
                await service.EditScheduledTaskAsync(DescriptionEdit(otherTask), new ScheduledTaskEditScope(_owner, task));
            }
            await using var verification = Context();
            Assert.That((await verification.AutomaticTask.FindAsync(task)).TaskDescription, Is.EqualTo("Edited within scope"));
            Assert.That((await verification.AutomaticTask.FindAsync(otherTask)).TaskDescription, Is.EqualTo("Original task"));
        }

        [Test]
        public async Task OwnershipChangeAfterAuthorizationPreventsEdit()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            await using var context = Context();
            var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            var authorization = new ScheduledTaskAuthorizationService(repository, null);
            var approval = await authorization.AuthorizeEditAsync(_owner.ToString(), task.ToString());
            Assert.That(approval.IsAuthorized, Is.True);
            await using (var change = Context())
                await change.AutomaticTask.Where(x => x.AutomaticTaskId == task)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.UserId, _other));
            var error = Assert.ThrowsAsync<ServiceException>(() => repository.EditScheduledTaskAsync(DescriptionEdit(task), approval.Scope));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await using var verification = Context();
            Assert.That((await verification.AutomaticTask.FindAsync(task)).TaskDescription, Is.EqualTo("Original task"));
        }

        [Test]
        public async Task ReplacementCategoryOutsideScopeCannotBeWritten()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            await using var context = Context();
            var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            var model = new ClientEditScheduledTask
            {
                TaskId = task.ToString(), SpendTypeId = int.MaxValue,
                ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.SpendTypeId }
            };
            var error = Assert.ThrowsAsync<ServiceException>(() => repository.EditScheduledTaskAsync(model,
                new ScheduledTaskEditScope(_owner, task)));
            Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            await using var verification = Context();
            Assert.That((await verification.AutomaticTask.FindAsync(task)).SpendTypeId, Is.EqualTo(3));
        }

        [Test]
        public async Task EditJoinsCallerTransactionAndRetainsOwnershipLockUntilCommit()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            await using var context = Context();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
            await repository.EditScheduledTaskAsync(DescriptionEdit(task), new ScheduledTaskEditScope(_owner, task));
            Assert.That(context.Database.CurrentTransaction, Is.SameAs(transaction));

            await using var change = Context();
            await change.Database.OpenConnectionAsync();
            await change.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 500");
            var blocked = Assert.CatchAsync<Exception>(() =>
                change.AutomaticTask.Where(x => x.AutomaticTaskId == task)
                    .ExecuteUpdateAsync(set => set.SetProperty(x => x.UserId, _other)));
            Assert.That(blocked.GetBaseException(), Is.TypeOf<Microsoft.Data.SqlClient.SqlException>());
            Assert.That(((Microsoft.Data.SqlClient.SqlException)blocked.GetBaseException()).Number, Is.EqualTo(1222));
            await transaction.CommitAsync();
            Assert.That(await change.AutomaticTask.Where(x => x.AutomaticTaskId == task)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.UserId, _other)), Is.EqualTo(1));
        }

        [Test]
        public async Task CategoryAssignmentIsRequiredAgainAtWriteTime()
        {
            var task = await NewScheduledTask(ScheduledTaskFrequencyType.Monthly, 28);
            int categoryId;
            await using (var seed = Context())
            {
                var category = new EFDataAccess.Models.SpendType { Name = "Scoped edit category" };
                seed.Add(category);
                await seed.SaveChangesAsync();
                categoryId = category.SpendTypeId;
                seed.Add(new EFDataAccess.Models.UserSpendType { UserId = _owner, SpendTypeId = categoryId });
                await seed.SaveChangesAsync();
            }
            var scope = new ScheduledTaskEditScope(_owner, task);
            var model = new ClientEditScheduledTask
            {
                TaskId = task.ToString(), SpendTypeId = categoryId,
                ModifyList = new[] { ClientEditScheduledTask.ScheduledTaskField.SpendTypeId }
            };
            await using (var context = Context())
                await new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance)
                    .EditScheduledTaskAsync(model, scope);
            await using (var revoke = Context())
            {
                await revoke.UserSpendType.Where(x => x.UserId == _owner && x.SpendTypeId == categoryId).ExecuteDeleteAsync();
                revoke.Add(new EFDataAccess.Models.UserSpendType { UserId = _other, SpendTypeId = categoryId });
                await revoke.SaveChangesAsync();
            }
            await using (var context = Context())
            {
                var repository = new EFAutomaticTaskRepository(context, NullLogger<EFAutomaticTaskRepository>.Instance);
                var error = Assert.ThrowsAsync<ServiceException>(() => repository.EditScheduledTaskAsync(model, scope));
                Assert.That(error.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
            await using var verification = Context();
            Assert.That((await verification.AutomaticTask.FindAsync(task)).SpendTypeId, Is.EqualTo(categoryId));
        }
    }
}
