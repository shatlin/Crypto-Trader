using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m19 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncludedForTrading",
                table: "MyCoins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PercAboveDayLowToSell",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PercBelowDayHighToBuy",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DivideHighAndAverageBy",
                table: "Config",
                type: "decimal(18,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncludedForTrading",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "PercAboveDayLowToSell",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "PercBelowDayHighToBuy",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DivideHighAndAverageBy",
                table: "Config");

        
        }
    }
}
