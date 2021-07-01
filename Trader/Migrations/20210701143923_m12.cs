using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isBuyCostAccurated",
                table: "PlayerTrades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isSellAmountAccurated",
                table: "PlayerTrades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isBuyCostAccurated",
                table: "Player",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isSellAmountAccurated",
                table: "Player",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isBuyCostAccurated",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isSellAmountAccurated",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isBuyCostAccurated",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "isSellAmountAccurated",
                table: "Player");
        }
    }
}
