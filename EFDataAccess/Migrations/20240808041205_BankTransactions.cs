using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class BankTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankTransactionId",
                table: "SpendOnPeriod",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankTrxFinancialEntityId",
                table: "SpendOnPeriod",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IsoCode",
                table: "Currency",
                type: "varchar(3)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankTransaction",
                columns: table => new
                {
                    BankTransactionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FinancialEntityId = table.Column<int>(type: "int", nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OriginalAccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransaction", x => new { x.BankTransactionId, x.FinancialEntityId });
                    table.ForeignKey(
                        name: "BankTransaction_FK_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currency",
                        principalColumn: "CurrencyId");
                    table.ForeignKey(
                        name: "BankTransaction_FK_FinancialEntityId",
                        column: x => x.FinancialEntityId,
                        principalTable: "FinancialEntity",
                        principalColumn: "FinancialEntityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "BankTransaction_FK_OriginalAccountId",
                        column: x => x.OriginalAccountId,
                        principalTable: "Account",
                        principalColumn: "AccountId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpendOnPeriod_BankTransactionId_BankTrxFinancialEntityId",
                table: "SpendOnPeriod",
                columns: new[] { "BankTransactionId", "BankTrxFinancialEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_CurrencyId",
                table: "BankTransaction",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_FinancialEntityId",
                table: "BankTransaction",
                column: "FinancialEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_OriginalAccountId",
                table: "BankTransaction",
                column: "OriginalAccountId");

            migrationBuilder.AddForeignKey(
                name: "SpendOnPeriod_FK_BankTransaction",
                table: "SpendOnPeriod",
                columns: new[] { "BankTransactionId", "BankTrxFinancialEntityId" },
                principalTable: "BankTransaction",
                principalColumns: new[] { "BankTransactionId", "FinancialEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "SpendOnPeriod_FK_BankTransaction",
                table: "SpendOnPeriod");

            migrationBuilder.DropTable(
                name: "BankTransaction");

            migrationBuilder.DropIndex(
                name: "IX_SpendOnPeriod_BankTransactionId_BankTrxFinancialEntityId",
                table: "SpendOnPeriod");

            migrationBuilder.DropColumn(
                name: "BankTransactionId",
                table: "SpendOnPeriod");

            migrationBuilder.DropColumn(
                name: "BankTrxFinancialEntityId",
                table: "SpendOnPeriod");

            migrationBuilder.DropColumn(
                name: "IsoCode",
                table: "Currency");
        }
    }
}
