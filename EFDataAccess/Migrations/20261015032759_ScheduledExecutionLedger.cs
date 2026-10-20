using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledExecutionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledTaskOccurrence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledLocalDate = table.Column<DateTime>(type: "date", nullable: false),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskOccurrence", x => x.Id);
                    table.CheckConstraint("CK_ScheduledTaskOccurrence_Date", "CONVERT(date, [PostingDate]) = [ScheduledLocalDate]");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskOccurrence_AppUser_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUser",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskOccurrence_AutomaticTask_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId");
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTaskCompletion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurrenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PostingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskCompletion", x => x.Id);
                    table.CheckConstraint("CK_ScheduledTaskCompletion_Identity", "([OccurrenceId] IS NOT NULL AND [ManualRequestId] IS NULL) OR ([OccurrenceId] IS NULL AND [ManualRequestId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskCompletion_AppUser_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AppUser",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskCompletion_AutomaticTask_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskCompletion_ScheduledTaskOccurrence_OccurrenceId",
                        column: x => x.OccurrenceId,
                        principalTable: "ScheduledTaskOccurrence",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskCompletion_ActorId_ManualRequestId",
                table: "ScheduledTaskCompletion",
                columns: new[] { "ActorId", "ManualRequestId" },
                unique: true,
                filter: "[ManualRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskCompletion_OccurrenceId",
                table: "ScheduledTaskCompletion",
                column: "OccurrenceId",
                unique: true,
                filter: "[OccurrenceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskCompletion_TaskId",
                table: "ScheduledTaskCompletion",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskOccurrence_DueAtUtc",
                table: "ScheduledTaskOccurrence",
                column: "DueAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskOccurrence_OwnerId",
                table: "ScheduledTaskOccurrence",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskOccurrence_TaskId_ScheduledLocalDate",
                table: "ScheduledTaskOccurrence",
                columns: new[] { "TaskId", "ScheduledLocalDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTaskCompletion");

            migrationBuilder.DropTable(
                name: "ScheduledTaskOccurrence");
        }
    }
}
