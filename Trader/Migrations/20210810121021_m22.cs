using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m22 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowDetailedNoLogs",
                table: "Config",
                newName: "ShowDetailedNoBuyLogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowDetailedNoBuyLogs",
                table: "Config",
                newName: "ShowDetailedNoLogs");
        }
    }
}
