using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m28 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SellWhenNotSoldForDays",
                table: "TradeBotHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellWhenNotSoldForDays",
                table: "TradeBot",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellWhenNotSoldForDays",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "SellWhenNotSoldForDays",
                table: "TradeBot");
        }
    }
}
