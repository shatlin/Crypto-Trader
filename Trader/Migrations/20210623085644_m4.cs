using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LastRoundProfitPerc",
                table: "PlayerTrades",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LastRoundProfitPerc",
                table: "Player",
                type: "decimal(30,12)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRoundProfitPerc",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "LastRoundProfitPerc",
                table: "Player");
        }
    }
}
