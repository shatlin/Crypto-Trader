using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m47 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DayVolume",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,7)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayTradeCount",
                table: "MyCoins",
                type: "decimal(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,7)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "DayVolume",
                table: "MyCoins",
                type: "decimal(18,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DayTradeCount",
                table: "MyCoins",
                type: "decimal(18,7)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(30,12)");
        }
    }
}
