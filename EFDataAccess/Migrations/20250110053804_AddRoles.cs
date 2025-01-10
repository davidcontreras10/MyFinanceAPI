using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "AppUser_FK_CreatedByUserId",
                table: "AppUser");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "AppUser",
                newName: "AppUserUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AppUser_CreatedByUserId",
                table: "AppUser",
                newName: "IX_AppUser_AppUserUserId");

            migrationBuilder.CreateTable(
                name: "AppRole",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.RoleId, x.UserId });
                    table.ForeignKey(
                        name: "FK_UserRole_AppRole_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AppRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_AppUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_UserId",
                table: "UserRole",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUser_AppUser_AppUserUserId",
                table: "AppUser",
                column: "AppUserUserId",
                principalTable: "AppUser",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUser_AppUser_AppUserUserId",
                table: "AppUser");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "AppRole");

            migrationBuilder.RenameColumn(
                name: "AppUserUserId",
                table: "AppUser",
                newName: "CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AppUser_AppUserUserId",
                table: "AppUser",
                newName: "IX_AppUser_CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "AppUser_FK_CreatedByUserId",
                table: "AppUser",
                column: "CreatedByUserId",
                principalTable: "AppUser",
                principalColumn: "UserId");
        }
    }
}
