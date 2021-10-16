using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m49 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TwoDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ThirtyMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OneWeekChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OneHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OneDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FourHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FiveMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FifteenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoinSymbol",
                table: "MyCoins",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DayHighPrice",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayLowPrice",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayOpenPrice",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCap",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecisionDecimals",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoinSymbol",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DayHighPrice",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DayLowPrice",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "DayOpenPrice",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "MarketCap",
                table: "MyCoins");

            migrationBuilder.DropColumn(
                name: "PrecisionDecimals",
                table: "MyCoins");

            migrationBuilder.AlterColumn<decimal>(
                name: "TwoDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ThirtyMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OneWeekChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OneHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OneDayChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FourHourChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FiveMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "FifteenMinChange",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");
        }
    }
}
