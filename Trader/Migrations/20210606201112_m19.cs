using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalCurrentProft",
                table: "TradeBotHistory",
                newName: "TotalSoldAmount");

            migrationBuilder.RenameColumn(
                name: "TotalCurrentProft",
                table: "TradeBot",
                newName: "TotalSoldAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableAmountForTrading",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingCommision",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantitySold",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldCommision",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldPricePricePerCoin",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCurrentProfit",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableAmountForTrading",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingCommision",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QuantitySold",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldCommision",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SoldPricePricePerCoin",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCurrentProfit",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableAmountForTrading",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "BuyingCommision",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "QuantitySold",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "SoldCommision",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "SoldPricePricePerCoin",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "TotalCurrentProfit",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "AvailableAmountForTrading",
                table: "TradeBot");

            migrationBuilder.DropColumn(
                name: "BuyingCommision",
                table: "TradeBot");

            migrationBuilder.DropColumn(
                name: "QuantitySold",
                table: "TradeBot");

            migrationBuilder.DropColumn(
                name: "SoldCommision",
                table: "TradeBot");

            migrationBuilder.DropColumn(
                name: "SoldPricePricePerCoin",
                table: "TradeBot");

            migrationBuilder.DropColumn(
                name: "TotalCurrentProfit",
                table: "TradeBot");

            migrationBuilder.RenameColumn(
                name: "TotalSoldAmount",
                table: "TradeBotHistory",
                newName: "TotalCurrentProft");

            migrationBuilder.RenameColumn(
                name: "TotalSoldAmount",
                table: "TradeBot",
                newName: "TotalCurrentProft");
        }
    }
}
