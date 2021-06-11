using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecordedTime",
                table: "DailyCandle");

            migrationBuilder.DropColumn(
                name: "RecordedTime",
                table: "Candle");

            migrationBuilder.AddColumn<string>(
                name: "BuyOrSell",
                table: "TradeBotHistory",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyOrSell",
                table: "TradeBot",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyOrSell",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "BuyOrSell",
                table: "TradeBot");

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedTime",
                table: "DailyCandle",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "RecordedTime",
                table: "Candle",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
