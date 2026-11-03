using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledTaskExecutionAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledTaskExecutionAttempt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "date", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FailureStage = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FailureType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskExecutionAttempt", x => x.Id);
                    table.CheckConstraint("CK_ScheduledTaskExecutionAttempt_Identity", "([OccurrenceId] IS NOT NULL AND [ManualRequestId] IS NULL) OR ([OccurrenceId] IS NULL AND [ManualRequestId] IS NOT NULL)");
                    table.CheckConstraint("CK_ScheduledTaskExecutionAttempt_Number", "[AttemptNumber] > 0");
                    table.CheckConstraint("CK_ScheduledTaskExecutionAttempt_Status", "([Status] = 1 AND [EndedAtUtc] IS NULL) OR ([Status] IN (2, 3, 4) AND [EndedAtUtc] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskExecutionAttempt_AppUser_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AppUser",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskExecutionAttempt_AppUser_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUser",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskExecutionAttempt_AutomaticTask_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskExecutionAttempt_ScheduledTaskOccurrence_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "ScheduledTaskOccurrence",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskExecutionAttempt_ActorId_ManualRequestId_AttemptNumber",
                table: "ScheduledTaskExecutionAttempt",
                columns: new[] { "ActorId", "ManualRequestId", "AttemptNumber" },
                unique: true,
                filter: "[ManualRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskExecutionAttempt_OccurrenceId_AttemptNumber",
                table: "ScheduledTaskExecutionAttempt",
                columns: new[] { "OccurrenceId", "AttemptNumber" },
                unique: true,
                filter: "[OccurrenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskExecutionAttempt_OwnerId",
                table: "ScheduledTaskExecutionAttempt",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskExecutionAttempt_TaskId_StartedAtUtc",
                table: "ScheduledTaskExecutionAttempt",
                columns: new[] { "TaskId", "StartedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTaskExecutionAttempt");
        }
    }
}
