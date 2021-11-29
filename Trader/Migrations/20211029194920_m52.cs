using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m52 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BuyAtPrice",
                table: "Player",
                type: "decimal(30,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SellAtPrice",
                table: "Player",
                type: "decimal(30,12)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyAtPrice",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "SellAtPrice",
                table: "Player");
        }
    }
}
