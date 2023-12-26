using Microsoft.EntityFrameworkCore.Migrations;

namespace EFDataAccess.Migrations
{
    public partial class SpendDependencyFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SpendDependencies_DependencySpendId",
                table: "SpendDependencies",
                column: "DependencySpendId");

            migrationBuilder.AddForeignKey(
                name: "SpendDependencies_Dep_FK_SpendId",
                table: "SpendDependencies",
                column: "DependencySpendId",
                principalTable: "Spend",
                principalColumn: "SpendId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "SpendDependencies_Dep_FK_SpendId",
                table: "SpendDependencies");

            migrationBuilder.DropIndex(
                name: "IX_SpendDependencies_DependencySpendId",
                table: "SpendDependencies");
        }
    }
}
