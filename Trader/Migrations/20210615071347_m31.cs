using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m31 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIsAbove",
                table: "TradeBotHistory",
                newName: "SellWhenProfitPercentageGoesBelow");

            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIsAbove",
                table: "TradeBot",
                newName: "SellWhenProfitPercentageGoesBelow");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageGoesBelow",
                table: "TradeBotHistory",
                newName: "SellWhenProfitPercentageIsAbove");

            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageGoesBelow",
                table: "TradeBot",
                newName: "SellWhenProfitPercentageIsAbove");
        }
    }
}
