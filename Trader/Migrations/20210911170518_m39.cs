using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m39 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CrashSell",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SellWhenAllBotsAtLossBelow",
                table: "Config",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldSellWhenAllBotsAtLoss",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CrashSell",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "SellWhenAllBotsAtLossBelow",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShouldSellWhenAllBotsAtLoss",
                table: "Config");
        }
    }
}
