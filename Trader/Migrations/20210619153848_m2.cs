using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "SellWhenNotSoldForDays",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "SellWhenNotSoldForDays",
                table: "Player");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "PlayerTrades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellWhenNotSoldForDays",
                table: "PlayerTrades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SellWhenNotSoldForDays",
                table: "Player",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
