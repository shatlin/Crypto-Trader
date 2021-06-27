using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandleBackUp",
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
                    table.PrimaryKey("PK_CandleBackUp", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandleBackUp");
        }
    }
}
