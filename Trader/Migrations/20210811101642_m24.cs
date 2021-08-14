using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m24 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowScalpSellLogs",
                table: "Config",
                newName: "ShowNoScalpBuyLogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowNoScalpBuyLogs",
                table: "Config",
                newName: "ShowScalpSellLogs");
        }
    }
}
