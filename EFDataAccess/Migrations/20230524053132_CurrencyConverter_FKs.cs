using Microsoft.EntityFrameworkCore.Migrations;

namespace EFDataAccess.Migrations
{
    public partial class CurrencyConverter_FKs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CurrencyConverter_CurrencyIdOne",
                table: "CurrencyConverter",
                column: "CurrencyIdOne");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyConverter_CurrencyIdTwo",
                table: "CurrencyConverter",
                column: "CurrencyIdTwo");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyConverter_Currency_CurrencyIdOne",
                table: "CurrencyConverter",
                column: "CurrencyIdOne",
                principalTable: "Currency",
                principalColumn: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyConverter_Currency_CurrencyIdTwo",
                table: "CurrencyConverter",
                column: "CurrencyIdTwo",
                principalTable: "Currency",
                principalColumn: "CurrencyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyConverter_Currency_CurrencyIdOne",
                table: "CurrencyConverter");

            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyConverter_Currency_CurrencyIdTwo",
                table: "CurrencyConverter");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyConverter_CurrencyIdOne",
                table: "CurrencyConverter");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyConverter_CurrencyIdTwo",
                table: "CurrencyConverter");
        }
    }
}
