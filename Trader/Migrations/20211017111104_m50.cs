using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m50 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MyCoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoinName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoinSymbol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TradePrecision = table.Column<int>(type: "int", nullable: false),
                    IsIncludedForTrading = table.Column<bool>(type: "bit", nullable: false),
                    ClimbingFast = table.Column<bool>(type: "bit", nullable: false),
                    ClimbedHigh = table.Column<bool>(type: "bit", nullable: false),
                    SuperHigh = table.Column<bool>(type: "bit", nullable: false),
                    PercBelowDayHighToBuy = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    PercAboveDayLowToSell = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    ForceBuy = table.Column<bool>(type: "bit", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    DayTradeCount = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayVolume = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayVolumeUSDT = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayOpenPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayHighPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayLowPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    FiveMinChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TenMinChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    FifteenMinChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    ThirtyMinChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    FourtyFiveMinChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    OneHourChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    FourHourChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TwentyFourHourChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    FortyEightHourChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    OneWeekChange = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    PrecisionDecimals = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    MarketCap = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TradeSuggestion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MyCoins", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MyCoins");
        }
    }
}
