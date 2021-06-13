using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m30 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalExpectedProfit",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalExpectedProfit",
                table: "TradeBot",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalExpectedProfit",
                table: "TradeBotHistory");

            migrationBuilder.DropColumn(
                name: "TotalExpectedProfit",
                table: "TradeBot");
        }
    }
}
