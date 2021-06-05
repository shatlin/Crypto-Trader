using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceCurrentSet",
                table: "Counter",
                newName: "DailyCandleCurrentSet");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DailyCandleCurrentSet",
                table: "Counter",
                newName: "PriceCurrentSet");
        }
    }
}
