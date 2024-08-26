using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFDataAccess.Migrations
{
    /// <inheritdoc />
    public partial class DefineAppTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppTransfer",
                columns: table => new
                {
                    SourceAppTrxId = table.Column<int>(type: "int", nullable: false),
                    DestinationAppTrxId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppTransfer", x => new { x.SourceAppTrxId, x.DestinationAppTrxId })
                        .Annotation("SqlServer:Clustered", true);
                    table.ForeignKey(
                        name: "AppTransfer_FK_DestinationAppTrxId",
                        column: x => x.DestinationAppTrxId,
                        principalTable: "Spend",
                        principalColumn: "SpendId");
                    table.ForeignKey(
                        name: "AppTransfer_FK_SourceAppTrxId",
                        column: x => x.SourceAppTrxId,
                        principalTable: "Spend",
                        principalColumn: "SpendId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppTransfer_DestinationAppTrxId",
                table: "AppTransfer",
                column: "DestinationAppTrxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppTransfer_SourceAppTrxId",
                table: "AppTransfer",
                column: "SourceAppTrxId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppTransfer");
        }
    }
}
