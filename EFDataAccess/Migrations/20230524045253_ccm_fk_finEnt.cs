using Microsoft.EntityFrameworkCore.Migrations;

namespace EFDataAccess.Migrations
{
    public partial class ccm_fk_finEnt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "BaseBudget",
                table: "Account",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyConverterMethod_FinancialEntityId",
                table: "CurrencyConverterMethod",
                column: "FinancialEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_CurrencyConverterMethod_FinancialEntity_FinancialEntityId",
                table: "CurrencyConverterMethod",
                column: "FinancialEntityId",
                principalTable: "FinancialEntity",
                principalColumn: "FinancialEntityId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurrencyConverterMethod_FinancialEntity_FinancialEntityId",
                table: "CurrencyConverterMethod");

            migrationBuilder.DropIndex(
                name: "IX_CurrencyConverterMethod_FinancialEntityId",
                table: "CurrencyConverterMethod");

            migrationBuilder.AlterColumn<double>(
                name: "BaseBudget",
                table: "Account",
                type: "float",
                nullable: true,
                oldClrType: typeof(float),
                oldNullable: true);
        }
    }
}
