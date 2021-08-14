using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedBuyLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedBuyingFlowLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedNoLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedNoSellLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedSellLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowDetailedSellingFlowLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowDetailedBuyLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowDetailedBuyingFlowLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowDetailedNoLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowDetailedNoSellLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowDetailedSellLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowDetailedSellingFlowLogs",
                table: "Config");
        }
    }
}
