using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEFDebtRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreditorDebtRequestId",
                table: "Spend",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DebtorDebtRequestId",
                table: "Spend",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DebtRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,5)", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    CreditorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DebtorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DebtorStatus = table.Column<int>(type: "int", nullable: false),
                    CreditorStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtRequest", x => x.Id);
                    table.ForeignKey(
                        name: "DebtRequest_FK_CreditorId",
                        column: x => x.CreditorId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "DebtRequest_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "DebtRequest_FK_DebtorId",
                        column: x => x.DebtorId,
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spend_CreditorDebtRequestId",
                table: "Spend",
                column: "CreditorDebtRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Spend_DebtorDebtRequestId",
                table: "Spend",
                column: "DebtorDebtRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtRequest_CreditorId",
                table: "DebtRequest",
                column: "CreditorId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtRequest_CurrencyId",
                table: "DebtRequest",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_DebtRequest_DebtorId",
                table: "DebtRequest",
                column: "DebtorId");

            migrationBuilder.AddForeignKey(
                name: "Spend_FK_CreditorDebtRequestId",
                table: "Spend",
                column: "CreditorDebtRequestId",
                principalTable: "DebtRequest",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "Spend_FK_DebtorDebtRequestId",
                table: "Spend",
                column: "DebtorDebtRequestId",
                principalTable: "DebtRequest",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Spend_FK_CreditorDebtRequestId",
                table: "Spend");

            migrationBuilder.DropForeignKey(
                name: "Spend_FK_DebtorDebtRequestId",
                table: "Spend");

            migrationBuilder.DropTable(
                name: "DebtRequest");

            migrationBuilder.DropIndex(
                name: "IX_Spend_CreditorDebtRequestId",
                table: "Spend");

            migrationBuilder.DropIndex(
                name: "IX_Spend_DebtorDebtRequestId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "CreditorDebtRequestId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "DebtorDebtRequestId",
                table: "Spend");
        }
    }
}
