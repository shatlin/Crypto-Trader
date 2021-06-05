using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m16 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCurrentValue",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCurrentProft",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBuyCost",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "SellWhenProfitPercentageIs",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityBought",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalAllocatedValue",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DayLow",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "DayHigh",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPricePerCoin",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyWhenGoneDownByPercentFromDayHigh",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPricePerCoin",
                table: "TradeBotHistory",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCurrentValue",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalCurrentProft",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalBuyCost",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellWhenProfitPercentageIs",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityBought",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalAllocatedValue",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayLow",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayHigh",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPricePerCoin",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyWhenGoneDownByPercentFromDayHigh",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BuyPricePerCoin",
                table: "TradeBotHistory",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");
        }
    }
}
