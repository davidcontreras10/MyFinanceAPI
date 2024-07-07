using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BankTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransactionId",
                table: "Spend",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankTrxFinancialEntityId",
                table: "Spend",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankTransaction",
                columns: table => new
                {
                    BankTransactionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinancialEntityId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransaction", x => new { x.BankTransactionId, x.FinancialEntityId });
                    table.ForeignKey(
                        name: "BankTransaction_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "BankTransaction_FK_FinancialEntityId",
                        column: x => x.FinancialEntityId,
                        principalTable: "FinancialEntity",
                        principalColumn: "FinancialEntityId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Spend_BankTransactionId_BankTrxFinancialEntityId",
                table: "Spend",
                columns: new[] { "BankTransactionId", "BankTrxFinancialEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_CurrencyId",
                table: "BankTransaction",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_FinancialEntityId",
                table: "BankTransaction",
                column: "FinancialEntityId");

            migrationBuilder.AddForeignKey(
                name: "Spend_FK_BankTransaction",
                table: "Spend",
                columns: new[] { "BankTransactionId", "BankTrxFinancialEntityId" },
                principalTable: "BankTransaction",
                principalColumns: new[] { "BankTransactionId", "FinancialEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Spend_FK_BankTransaction",
                table: "Spend");

            migrationBuilder.DropTable(
                name: "BankTransaction");

            migrationBuilder.DropIndex(
                name: "IX_Spend_BankTransactionId_BankTrxFinancialEntityId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "BankTransactionId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "BankTrxFinancialEntityId",
                table: "Spend");
        }
    }
}
