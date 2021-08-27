using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m28 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinSellAbovePerc",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReducePriceDiffPercBy",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReduceSellAboveAtMinute",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ReduceSellAboveBy",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ReduceSellAboveFromSecond",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ReduceSellAboveToSecond",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinSellAbovePerc",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ReducePriceDiffPercBy",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ReduceSellAboveAtMinute",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ReduceSellAboveBy",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ReduceSellAboveFromSecond",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ReduceSellAboveToSecond",
                table: "Config");
        }
    }
}
