using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTime",
                table: "TradeBotHistory",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CandleOpenTime",
                table: "TradeBot",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandleOpenTime",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "CandleOpenTime",
                table: "TradeBot");
        }
    }
}
