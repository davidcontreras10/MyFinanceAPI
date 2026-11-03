using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledUserTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SchedulingTimeZoneId",
                table: "AppUser",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "America/Costa_Rica");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SchedulingTimeZoneId",
                table: "AppUser");
        }
    }
}
