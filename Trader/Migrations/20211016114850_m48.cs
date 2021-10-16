using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m48 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Coin",
                table: "MyCoins",
                newName: "TradeSuggestion");

            migrationBuilder.AddColumn<string>(
                name: "CoinName",
                table: "MyCoins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DayVolumeUSDT",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FifteenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FiveMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FourHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OneDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OneHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OneWeekChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Pair",
                table: "MyCoins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ThirtyMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TwoDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoinName",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DayVolumeUSDT",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "FifteenMinChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "FiveMinChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "FourHourChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "OneDayChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "OneHourChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "OneWeekChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "Pair",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "TenMinChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "ThirtyMinChange",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "TwoDayChange",
                table: "MyCoins");

            migrationBuilder.RenameColumn(
                name: "TradeSuggestion",
                table: "MyCoins",
                newName: "Coin");
        }
    }
}
