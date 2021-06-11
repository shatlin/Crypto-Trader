using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m24 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CandleyOpenTimeAtBuy",
                table: "TradeBot",
                newName: "CandleOpenTimeAtBuy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CandleOpenTimeAtBuy",
                table: "TradeBot",
                newName: "CandleyOpenTimeAtBuy");
        }
    }
}
