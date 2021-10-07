using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m45 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DayTradeCount",
                table: "MyCoins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DayVolume",
                table: "MyCoins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rank",
                table: "MyCoins",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayTradeCount",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DayVolume",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "Rank",
                table: "MyCoins");
        }
    }
}
