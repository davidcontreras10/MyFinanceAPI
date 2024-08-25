using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountFromBankTrx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "BankTransaction_FK_OriginalAccountId",
                table: "BankTransaction");

            migrationBuilder.DropIndex(
                name: "IX_BankTransaction_OriginalAccountId",
                table: "BankTransaction");

            migrationBuilder.DropColumn(
                name: "OriginalAccountId",
                table: "BankTransaction");

            migrationBuilder.AddColumn<string>(
                name: "FileDescription",
                table: "BankTransaction",
                type: "nvarchar(4000)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileDescription",
                table: "BankTransaction");

            migrationBuilder.AddColumn<int>(
                name: "OriginalAccountId",
                table: "BankTransaction",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankTransaction_OriginalAccountId",
                table: "BankTransaction",
                column: "OriginalAccountId");

            migrationBuilder.AddForeignKey(
                name: "BankTransaction_FK_OriginalAccountId",
                table: "BankTransaction",
                column: "OriginalAccountId",
                principalTable: "Account",
                principalColumn: "AccountId");
        }
    }
}
