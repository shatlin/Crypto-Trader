using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m22 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferenceOrder",
                table: "MyTradeFavouredCoins");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferenceOrder",
                table: "MyTradeFavouredCoins",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
