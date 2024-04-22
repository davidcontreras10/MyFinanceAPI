using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DefaultSelAccCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultSelectCurrencyId",
                table: "Account",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Account_DefaultSelectCurrencyId",
                table: "Account",
                column: "DefaultSelectCurrencyId");

            migrationBuilder.AddForeignKey(
                name: "Account_FK_DefaultSelectCurrencyId",
                table: "Account",
                column: "DefaultSelectCurrencyId",
                principalTable: "Currency",
                principalColumn: "CurrencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Account_FK_DefaultSelectCurrencyId",
                table: "Account");

            migrationBuilder.DropIndex(
                name: "IX_Account_DefaultSelectCurrencyId",
                table: "Account");

            migrationBuilder.DropColumn(
                name: "DefaultSelectCurrencyId",
                table: "Account");
        }
    }
}
