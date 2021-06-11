using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CandleOpenTime",
                table: "TradeBotHistory",
                newName: "CandleOpenTimeAtSell");

            migrationBuilder.RenameColumn(
                name: "CandleOpenTime",
                table: "TradeBot",
                newName: "CandleyOpenTimeAtBuy");

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtBuy",
                table: "TradeBotHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTimeAtSell",
                table: "TradeBot",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtBuy",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "CandleOpenTimeAtSell",
                table: "TradeBot");

            migrationBuilder.RenameColumn(
                name: "CandleOpenTimeAtSell",
                table: "TradeBotHistory",
                newName: "CandleOpenTime");

            migrationBuilder.RenameColumn(
                name: "CandleyOpenTimeAtBuy",
                table: "TradeBot",
                newName: "CandleOpenTime");
        }
    }
}
