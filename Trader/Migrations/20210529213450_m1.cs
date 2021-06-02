using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
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
                    Free = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Locked = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoughtPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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
                    RecordedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CloseTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuoteAssetVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NumberOfTrades = table.Column<int>(type: "int", nullable: false),
                    TakerBuyBaseAssetVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TakerBuyQuoteAssetVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DayLowPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DayHighPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DayVolume = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DayTradeCount = table.Column<int>(type: "int", nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceChangePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeightedAveragePercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousClosePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candle", x => x.Id);
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
                name: "MyTrade");
        }
    }
}
