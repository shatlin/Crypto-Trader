using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m36 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LossOrProfit",
                table: "PlayerHist",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleProfitOrLoss",
                table: "PlayerHist",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LossOrProfit",
                table: "Player",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SaleProfitOrLoss",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LossOrProfit",
                table: "PlayerHist");

            migrationBuilder.DropColumn(
                name: "SaleProfitOrLoss",
                table: "PlayerHist");

            migrationBuilder.DropColumn(
                name: "LossOrProfit",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "SaleProfitOrLoss",
                table: "Player");
        }
    }
}
