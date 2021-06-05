using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m18 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "TradeBotHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "TradeBot",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "TradeBot");
        }
    }
}
