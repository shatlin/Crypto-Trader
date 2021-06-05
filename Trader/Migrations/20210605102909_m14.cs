using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "TradeBot");

            migrationBuilder.CreateTable(
                name: "TradeBotHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Pair = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DayHigh = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    DayLow = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyWhenGoneDownByPercentFromDayHigh = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    SellWhenProfitPercentageIs = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyPricePerCoin = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    CurrentPricePerCoin = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    QuantityBought = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalBuyCost = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    TotalCurrentProft = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    OriginalAllocatedValue = table.Column<decimal>(type: "decimal(30,12)", nullable: false),
                    BuyTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeBotHistory", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradeBotHistory");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "TradeBot",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
