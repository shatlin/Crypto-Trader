using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m6 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageBuyingCoinPrice",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentCoinPrice",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Difference",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DifferencePercentage",
                table: "Balance",
                type: "decimal(18,9)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageBuyingCoinPrice",
                table: "Balance");

            migrationBuilder.DropColumn(
                name: "CurrentCoinPrice",
                table: "Balance");

            migrationBuilder.DropColumn(
                name: "Difference",
                table: "Balance");

            migrationBuilder.DropColumn(
                name: "DifferencePercentage",
                table: "Balance");
        }
    }
}
