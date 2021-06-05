using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m15 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActivelyTrading",
                table: "TradeBotHistory",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActivelyTrading",
                table: "TradeBot",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActivelyTrading",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "IsActivelyTrading",
                table: "TradeBot");
        }
    }
}
