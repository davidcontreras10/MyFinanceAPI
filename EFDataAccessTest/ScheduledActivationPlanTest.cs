using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ScheduledTaskActivation;

namespace EFDataAccessTest
{
    public sealed class ScheduledActivationPlanTest
    {
        private static readonly DateTimeOffset Now = new(2026, 1, 31, 18, 0, 0, TimeSpan.Zero);
        private const string Server = "(localdb)\\MSSQLLocalDB";
        private const string Database = "ActivationPlanUnitTest";

        private static ActivationPlan Selection() => new()
        {
            ExpectedServer = Server, ExpectedDatabase = Database, AdministratorId = Guid.NewGuid(),
            ActivationUtc = Now.AddDays(-1),
            Tasks = new[] { new ActivationTask { TaskId = Guid.NewGuid(), OwnerId = Guid.NewGuid() } }
        };

        [Test]
        public void ExplicitSelectionAndMatchingConnectionAreAccepted()
        {
            var plan = Selection();
            Assert.DoesNotThrow(() => ActivationRunner.ValidateSelection(plan, Now));
            Assert.DoesNotThrow(() => ActivationRunner.ValidateTarget(plan, $"Server={Server};Database={Database};Integrated Security=true"));
        }

        [TestCase("server")]
        [TestCase("database")]
        [TestCase("administrator")]
        [TestCase("boundary")]
        [TestCase("future")]
        [TestCase("offset")]
        [TestCase("empty")]
        [TestCase("duplicate")]
        [TestCase("owner")]
        [TestCase("task")]
        [TestCase("limit")]
        public void UnsafeSelectionsAreRejectedBeforeDatabaseAccess(string invalid)
        {
            var plan = Selection();
            plan = invalid switch
            {
                "server" => plan with { ExpectedServer = "" },
                "database" => plan with { ExpectedDatabase = "" },
                "administrator" => plan with { AdministratorId = Guid.Empty },
                "boundary" => plan with { ActivationUtc = default },
                "future" => plan with { ActivationUtc = Now.AddTicks(1) },
                "offset" => plan with { ActivationUtc = Now.AddDays(-1).ToOffset(TimeSpan.FromHours(-6)) },
                "empty" => plan with { Tasks = Array.Empty<ActivationTask>() },
                "duplicate" => plan with { Tasks = new[] { plan.Tasks[0], plan.Tasks[0] } },
                "owner" => plan with { Tasks = new[] { plan.Tasks[0] with { OwnerId = Guid.Empty } } },
                "task" => plan with { Tasks = new[] { plan.Tasks[0] with { TaskId = Guid.Empty } } },
                "limit" => plan with { Tasks = Enumerable.Range(0, 101).Select(_ => plan.Tasks[0] with { TaskId = Guid.NewGuid() }).ToArray() },
                _ => throw new ArgumentException("Unknown case")
            };
            Assert.Throws<ArgumentException>(() => ActivationRunner.ValidateSelection(plan, Now));
        }

        [TestCase("different-server", Database)]
        [TestCase(Server, "different-database")]
        public void ConnectionMustMatchReviewedTarget(string server, string database) =>
            Assert.Throws<ArgumentException>(() => ActivationRunner.ValidateTarget(Selection(),
                $"Server={server};Database={database};Integrated Security=true"));

        [Test]
        public void ApplyWithoutReviewCannotReachTheDatabase()
        {
            var runner = new ActivationRunner("invalid connection", TimeProvider.System);
            Assert.ThrowsAsync<ArgumentException>(() => runner.ApplyAsync(Selection()));
        }
    }
}
