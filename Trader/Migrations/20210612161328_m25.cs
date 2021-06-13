using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m25 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradeBotBackup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuyOrSell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayHigh = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayLow = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyWhenValuePercentageIsBelow = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellWhenProfitPercentageIsAbove = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyPricePerCoin = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CurrentPricePerCoin = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    QuantityBought = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyingCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SoldPricePricePerCoin = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    QuantitySold = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SoldCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalSoldAmount = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalBuyCost = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentProfit = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    AvailableAmountForTrading = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    OriginalAllocatedValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    IsActivelyTrading = table.Column<bool>(type: "bit", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandleOpenTimeAtBuy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandleOpenTimeAtSell = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeBotBackup", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeBotBackup");
        }
    }
}
