using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "API",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    secret = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_API", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Balance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Asset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Free = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Locked = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    TotBoughtPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    TotCurrentPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    AvgBuyCoinPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    CurrCoinPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DiffPerc = table.Column<decimal>(type: "decimal(18,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Balance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Candle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuoteAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    NumberOfTrades = table.Column<int>(type: "int", nullable: false),
                    TakerBuyBaseAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    TakerBuyQuoteAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayLowPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayHighPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    DayTradeCount = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    PriceChangePercent = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    WeightedAveragePercent = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    PreviousClosePrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Counter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsCandleBeingUpdated = table.Column<bool>(type: "bit", nullable: false),
                    IsDailyCandleBeingUpdated = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Counter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyCandle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuoteAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    NumberOfTrades = table.Column<int>(type: "int", nullable: false),
                    TakerBuyBaseAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    TakerBuyQuoteAssetVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayLowPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayHighPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    DayVolume = table.Column<decimal>(type: "decimal(23,4)", nullable: false),
                    DayTradeCount = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    PriceChangePercent = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    WeightedAveragePercent = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    PreviousClosePrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyCandle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MyCoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Coin = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyCoins", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MyTrade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    Commission = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CommissionAsset = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsBuyer = table.Column<bool>(type: "bit", nullable: false),
                    IsMaker = table.Column<bool>(type: "bit", nullable: false),
                    IsBestMatch = table.Column<bool>(type: "bit", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(30,12)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyTrade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Player",
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
                    BuyBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DontSellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellAbovePerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
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
                    IsTrading = table.Column<bool>(type: "bit", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellWhenNotSoldForDays = table.Column<int>(type: "int", nullable: false),
                    TotalExpectedProfit = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CandleOpenTimeAtBuy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandleOpenTimeAtSell = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyCandleId = table.Column<int>(type: "int", nullable: false),
                    SellCandleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerHist",
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
                    BuyBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DontSellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellAbovePerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
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
                    IsTrading = table.Column<bool>(type: "bit", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellWhenNotSoldForDays = table.Column<int>(type: "int", nullable: false),
                    TotalExpectedProfit = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CandleOpenTimeAtBuy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CandleOpenTimeAtSell = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyCandleId = table.Column<int>(type: "int", nullable: false),
                    SellCandleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerHist", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "API");

            migrationBuilder.DropTable(
                name: "Balance");

            migrationBuilder.DropTable(
                name: "Candle");

            migrationBuilder.DropTable(
                name: "Counter");

            migrationBuilder.DropTable(
                name: "DailyCandle");

            migrationBuilder.DropTable(
                name: "MyCoins");

            migrationBuilder.DropTable(
                name: "MyTrade");

            migrationBuilder.DropTable(
                name: "Player");

            migrationBuilder.DropTable(
                name: "PlayerHist");
        }
    }
}
