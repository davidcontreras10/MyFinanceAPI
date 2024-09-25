using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTrxConvValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyConverterMethodId",
                table: "Spend",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPurchase",
                table: "Spend",
                type: "bit",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Spend_CurrencyConverterMethodId",
                table: "Spend",
                column: "CurrencyConverterMethodId");

            migrationBuilder.AddForeignKey(
                name: "Spend_FK_CurrencyConverterMethodId",
                table: "Spend",
                column: "CurrencyConverterMethodId",
                principalTable: "CurrencyConverterMethod",
                principalColumn: "CurrencyConverterMethodId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "Spend_FK_CurrencyConverterMethodId",
                table: "Spend");

            migrationBuilder.DropIndex(
                name: "IX_Spend_CurrencyConverterMethodId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "CurrencyConverterMethodId",
                table: "Spend");

            migrationBuilder.DropColumn(
                name: "IsPurchase",
                table: "Spend");
        }
    }
}
