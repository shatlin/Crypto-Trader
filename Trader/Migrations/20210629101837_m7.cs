using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailableAmountForTrading",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "BuyPricePerCoin",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "BuyingCommision",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtBuy",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtSell",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "CurrentPricePerCoin",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "LossOrProfit",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "AvailableAmountForTrading",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "BuyPricePerCoin",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "BuyingCommision",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtBuy",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtSell",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "CurrentPricePerCoin",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "LossOrProfit",
                table: "Player");

            migrationBuilder.RenameColumn(
                name: "TotalSoldAmount",
                table: "PlayerTrades",
                newName: "TotalSellAmount");

            migrationBuilder.RenameColumn(
                name: "TotalExpectedProfit",
                table: "PlayerTrades",
                newName: "SellCommision");

            migrationBuilder.RenameColumn(
                name: "TotalCurrentProfit",
                table: "PlayerTrades",
                newName: "SellCoinPrice");

            migrationBuilder.RenameColumn(
                name: "SoldPricePricePerCoin",
                table: "PlayerTrades",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "SoldCommision",
                table: "PlayerTrades",
                newName: "ProfitLossAmt");

            migrationBuilder.RenameColumn(
                name: "SaleProfitOrLoss",
                table: "PlayerTrades",
                newName: "CurrentCoinPrice");

            migrationBuilder.RenameColumn(
                name: "QuantitySold",
                table: "PlayerTrades",
                newName: "BuyCommision");

            migrationBuilder.RenameColumn(
                name: "QuantityBought",
                table: "PlayerTrades",
                newName: "BuyCoinPrice");

            migrationBuilder.RenameColumn(
                name: "OriginalAllocatedValue",
                table: "PlayerTrades",
                newName: "AvailableAmountToBuy");

            migrationBuilder.RenameColumn(
                name: "TotalSoldAmount",
                table: "Player",
                newName: "TotalSellAmount");

            migrationBuilder.RenameColumn(
                name: "TotalExpectedProfit",
                table: "Player",
                newName: "SellCommision");

            migrationBuilder.RenameColumn(
                name: "TotalCurrentProfit",
                table: "Player",
                newName: "SellCoinPrice");

            migrationBuilder.RenameColumn(
                name: "SoldPricePricePerCoin",
                table: "Player",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "SoldCommision",
                table: "Player",
                newName: "ProfitLossAmt");

            migrationBuilder.RenameColumn(
                name: "SaleProfitOrLoss",
                table: "Player",
                newName: "CurrentCoinPrice");

            migrationBuilder.RenameColumn(
                name: "QuantitySold",
                table: "Player",
                newName: "BuyCommision");

            migrationBuilder.RenameColumn(
                name: "QuantityBought",
                table: "Player",
                newName: "BuyCoinPrice");

            migrationBuilder.RenameColumn(
                name: "OriginalAllocatedValue",
                table: "Player",
                newName: "AvailableAmountToBuy");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalSellAmount",
                table: "PlayerTrades",
                newName: "TotalSoldAmount");

            migrationBuilder.RenameColumn(
                name: "SellCommision",
                table: "PlayerTrades",
                newName: "TotalExpectedProfit");

            migrationBuilder.RenameColumn(
                name: "SellCoinPrice",
                table: "PlayerTrades",
                newName: "TotalCurrentProfit");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "PlayerTrades",
                newName: "SoldPricePricePerCoin");

            migrationBuilder.RenameColumn(
                name: "ProfitLossAmt",
                table: "PlayerTrades",
                newName: "SoldCommision");

            migrationBuilder.RenameColumn(
                name: "CurrentCoinPrice",
                table: "PlayerTrades",
                newName: "SaleProfitOrLoss");

            migrationBuilder.RenameColumn(
                name: "BuyCommision",
                table: "PlayerTrades",
                newName: "QuantitySold");

            migrationBuilder.RenameColumn(
                name: "BuyCoinPrice",
                table: "PlayerTrades",
                newName: "QuantityBought");

            migrationBuilder.RenameColumn(
                name: "AvailableAmountToBuy",
                table: "PlayerTrades",
                newName: "OriginalAllocatedValue");

            migrationBuilder.RenameColumn(
                name: "TotalSellAmount",
                table: "Player",
                newName: "TotalSoldAmount");

            migrationBuilder.RenameColumn(
                name: "SellCommision",
                table: "Player",
                newName: "TotalExpectedProfit");

            migrationBuilder.RenameColumn(
                name: "SellCoinPrice",
                table: "Player",
                newName: "TotalCurrentProfit");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "Player",
                newName: "SoldPricePricePerCoin");

            migrationBuilder.RenameColumn(
                name: "ProfitLossAmt",
                table: "Player",
                newName: "SoldCommision");

            migrationBuilder.RenameColumn(
                name: "CurrentCoinPrice",
                table: "Player",
                newName: "SaleProfitOrLoss");

            migrationBuilder.RenameColumn(
                name: "BuyCommision",
                table: "Player",
                newName: "QuantitySold");

            migrationBuilder.RenameColumn(
                name: "BuyCoinPrice",
                table: "Player",
                newName: "QuantityBought");

            migrationBuilder.RenameColumn(
                name: "AvailableAmountToBuy",
                table: "Player",
                newName: "OriginalAllocatedValue");

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableAmountForTrading",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "PlayerTrades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyPricePerCoin",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingCommision",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtBuy",
                table: "PlayerTrades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtSell",
                table: "PlayerTrades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "PlayerTrades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPricePerCoin",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LossOrProfit",
                table: "PlayerTrades",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AvailableAmountForTrading",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "Player",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyPricePerCoin",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyingCommision",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtBuy",
                table: "Player",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtSell",
                table: "Player",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Player",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPricePerCoin",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LossOrProfit",
                table: "Player",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
