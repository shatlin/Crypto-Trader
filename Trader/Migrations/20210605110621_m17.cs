using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m17 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIs",
                table: "TradeBotHistory",
                newName: "SellWhenProfitPercentageIsAbove");

            migrationBuilder.RenameColumn(
                name: "BuyWhenGoneDownByPercentFromDayHigh",
                table: "TradeBotHistory",
                newName: "BuyWhenValuePercentageIsBelow");

            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIs",
                table: "TradeBot",
                newName: "SellWhenProfitPercentageIsAbove");

            migrationBuilder.RenameColumn(
                name: "BuyWhenGoneDownByPercentFromDayHigh",
                table: "TradeBot",
                newName: "BuyWhenValuePercentageIsBelow");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIsAbove",
                table: "TradeBotHistory",
                newName: "SellWhenProfitPercentageIs");

            migrationBuilder.RenameColumn(
                name: "BuyWhenValuePercentageIsBelow",
                table: "TradeBotHistory",
                newName: "BuyWhenGoneDownByPercentFromDayHigh");

            migrationBuilder.RenameColumn(
                name: "SellWhenProfitPercentageIsAbove",
                table: "TradeBot",
                newName: "SellWhenProfitPercentageIs");

            migrationBuilder.RenameColumn(
                name: "BuyWhenValuePercentageIsBelow",
                table: "TradeBot",
                newName: "BuyWhenGoneDownByPercentFromDayHigh");
        }
    }
}
