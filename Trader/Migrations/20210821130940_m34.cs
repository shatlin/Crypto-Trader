using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m34 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinAllowedTradeCount",
                table: "Config",
                type: "decimal(18,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PlayerQA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsTrading = table.Column<bool>(type: "bit", nullable: false),
                    RepsTillCancelOrder = table.Column<int>(type: "int", nullable: false),
                    isBuyAllowed = table.Column<bool>(type: "bit", nullable: false),
                    isSellAllowed = table.Column<bool>(type: "bit", nullable: false),
                    DayHigh = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayLow = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellAbovePerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DontSellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    HardSellPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    isBuyOrderCompleted = table.Column<bool>(type: "bit", nullable: false),
                    BuyCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CurrentCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    LastRoundProfitPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalBuyCost = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalSellAmount = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    AvailableAmountToBuy = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SellOrderId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfitLossChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuyOrSell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfitLossAmt = table.Column<decimal>(type: "decimal(30,12)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerQA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerTradesQA",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsTrading = table.Column<bool>(type: "bit", nullable: false),
                    isSellAllowed = table.Column<bool>(type: "bit", nullable: false),
                    DayHigh = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayLow = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    HardSellPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    isBuyOrderCompleted = table.Column<bool>(type: "bit", nullable: false),
                    BuyBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellAbovePerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DontSellBelowPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CurrentCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellCoinPrice = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellCommision = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    isBuyAllowed = table.Column<bool>(type: "bit", nullable: false),
                    LastRoundProfitPerc = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    RepsTillCancelOrder = table.Column<int>(type: "int", nullable: false),
                    TotalBuyCost = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalSellAmount = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    AvailableAmountToBuy = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyOrderId = table.Column<long>(type: "bigint", nullable: false),
                    SellOrderId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfitLossChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BuyOrSell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProfitLossAmt = table.Column<decimal>(type: "decimal(30,12)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerTradesQA", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerQA");

            migrationBuilder.DropTable(
                name: "PlayerTradesQA");

            migrationBuilder.DropColumn(
                name: "MinAllowedTradeCount",
                table: "Config");
        }
    }
}
