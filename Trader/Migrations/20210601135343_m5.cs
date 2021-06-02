using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m5 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "WeightedAveragePercent",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Volume",
                table: "Candle",
                type: "decimal(23,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TakerBuyQuoteAssetVolume",
                table: "Candle",
                type: "decimal(23,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TakerBuyBaseAssetVolume",
                table: "Candle",
                type: "decimal(23,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuoteAssetVolume",
                table: "Candle",
                type: "decimal(23,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceChangePercent",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PreviousClosePrice",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpenPrice",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Open",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Low",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "High",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayVolume",
                table: "Candle",
                type: "decimal(23,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayLowPrice",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayHighPrice",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Close",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Change",
                table: "Candle",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "WeightedAveragePercent",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Volume",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(23,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TakerBuyQuoteAssetVolume",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(23,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TakerBuyBaseAssetVolume",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(23,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuoteAssetVolume",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(23,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PriceChangePercent",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PreviousClosePrice",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OpenPrice",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Open",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Low",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "High",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayVolume",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(23,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayLowPrice",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayHighPrice",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Close",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Change",
                table: "Candle",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");
        }
    }
}
