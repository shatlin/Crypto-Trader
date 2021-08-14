using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m23 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ShowDetailedSellingFlowLogs",
                table: "Config",
                newName: "ShowSellingFlowLogs");

            migrationBuilder.RenameColumn(
                name: "ShowDetailedSellLogs",
                table: "Config",
                newName: "ShowSellLogs");

            migrationBuilder.RenameColumn(
                name: "ShowDetailedNoSellLogs",
                table: "Config",
                newName: "ShowScalpSellLogs");

            migrationBuilder.RenameColumn(
                name: "ShowDetailedNoBuyLogs",
                table: "Config",
                newName: "ShowScalpBuyLogs");

            migrationBuilder.RenameColumn(
                name: "ShowDetailedBuyingFlowLogs",
                table: "Config",
                newName: "ShowNoSellLogs");

            migrationBuilder.RenameColumn(
                name: "ShowDetailedBuyLogs",
                table: "Config",
                newName: "ShowNoBuyLogs");

            migrationBuilder.AddColumn<bool>(
                name: "ShowBuyLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowBuyingFlowLogs",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowBuyLogs",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ShowBuyingFlowLogs",
                table: "Config");

            migrationBuilder.RenameColumn(
                name: "ShowSellingFlowLogs",
                table: "Config",
                newName: "ShowDetailedSellingFlowLogs");

            migrationBuilder.RenameColumn(
                name: "ShowSellLogs",
                table: "Config",
                newName: "ShowDetailedSellLogs");

            migrationBuilder.RenameColumn(
                name: "ShowScalpSellLogs",
                table: "Config",
                newName: "ShowDetailedNoSellLogs");

            migrationBuilder.RenameColumn(
                name: "ShowScalpBuyLogs",
                table: "Config",
                newName: "ShowDetailedNoBuyLogs");

            migrationBuilder.RenameColumn(
                name: "ShowNoSellLogs",
                table: "Config",
                newName: "ShowDetailedBuyingFlowLogs");

            migrationBuilder.RenameColumn(
                name: "ShowNoBuyLogs",
                table: "Config",
                newName: "ShowDetailedBuyLogs");
        }
    }
}
