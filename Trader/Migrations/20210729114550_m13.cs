using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HardSellPerc",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "isBuyOrderCompleted",
                table: "PlayerTrades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HardSellPerc",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "isBuyOrderCompleted",
                table: "Player",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HardSellPerc",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isBuyOrderCompleted",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "HardSellPerc",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "isBuyOrderCompleted",
                table: "Player");
        }
    }
}
