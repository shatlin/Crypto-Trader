using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TradePrecision",
                table: "MyCoins",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Config",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "IntervalMinutes", "MaxPauses" },
                values: new object[] { 5, 1 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TradePrecision",
                table: "MyCoins");

            migrationBuilder.UpdateData(
                table: "Config",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "IntervalMinutes", "MaxPauses" },
                values: new object[] { 15, 16 });
        }
    }
}
