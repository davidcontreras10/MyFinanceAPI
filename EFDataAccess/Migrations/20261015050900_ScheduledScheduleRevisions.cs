using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledScheduleRevisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledTaskSchedule",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextScanUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskSchedule", x => x.TaskId);
                    table.CheckConstraint("CK_ScheduledTaskSchedule_Cursor", "[NextScanUtc] >= [ActivatedAtUtc]");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskSchedule_AppUser_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AppUser",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskSchedule_AutomaticTask_TaskId",
                        column: x => x.TaskId,
                        principalTable: "AutomaticTask",
                        principalColumn: "AutomaticTaskId");
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTaskRevision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTaskRevision", x => x.Id);
                    table.CheckConstraint("CK_ScheduledTaskRevision_Window", "[EffectiveUntilUtc] IS NULL OR [EffectiveUntilUtc] > [EffectiveFromUtc]");
                    table.ForeignKey(
                        name: "FK_ScheduledTaskRevision_ScheduledTaskSchedule_TaskId",
                        column: x => x.TaskId,
                        principalTable: "ScheduledTaskSchedule",
                        principalColumn: "TaskId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskRevision_TaskId",
                table: "ScheduledTaskRevision",
                column: "TaskId",
                unique: true,
                filter: "[EffectiveUntilUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskRevision_TaskId_EffectiveFromUtc",
                table: "ScheduledTaskRevision",
                columns: new[] { "TaskId", "EffectiveFromUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskSchedule_NextScanUtc",
                table: "ScheduledTaskSchedule",
                column: "NextScanUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTaskSchedule_OwnerId",
                table: "ScheduledTaskSchedule",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTaskRevision");

            migrationBuilder.DropTable(
                name: "ScheduledTaskSchedule");
        }
    }
}
