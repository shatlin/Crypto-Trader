using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Locked",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Free",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BoughtPrice",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(15,3)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Locked",
                table: "Balance",
                type: "decimal(15,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Free",
                table: "Balance",
                type: "decimal(15,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "CurrentPrice",
                table: "Balance",
                type: "decimal(15,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");

            migrationBuilder.AlterColumn<decimal>(
                name: "BoughtPrice",
                table: "Balance",
                type: "decimal(15,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,9)");
        }
    }
}
