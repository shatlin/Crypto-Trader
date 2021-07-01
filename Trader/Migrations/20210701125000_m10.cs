using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SellCandleId",
                table: "PlayerTrades",
                newName: "SellOrderId");

            migrationBuilder.RenameColumn(
                name: "BuyCandleId",
                table: "PlayerTrades",
                newName: "BuyOrderId");

            migrationBuilder.RenameColumn(
                name: "SellCandleId",
                table: "Player",
                newName: "SellOrderId");

            migrationBuilder.RenameColumn(
                name: "BuyCandleId",
                table: "Player",
                newName: "BuyOrderId");

            migrationBuilder.AddColumn<bool>(
                name: "isSellAllowed",
                table: "PlayerTrades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isSellAllowed",
                table: "Player",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isSellAllowed",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isSellAllowed",
                table: "Player");

            migrationBuilder.RenameColumn(
                name: "SellOrderId",
                table: "PlayerTrades",
                newName: "SellCandleId");

            migrationBuilder.RenameColumn(
                name: "BuyOrderId",
                table: "PlayerTrades",
                newName: "BuyCandleId");

            migrationBuilder.RenameColumn(
                name: "SellOrderId",
                table: "Player",
                newName: "SellCandleId");

            migrationBuilder.RenameColumn(
                name: "BuyOrderId",
                table: "Player",
                newName: "BuyCandleId");
        }
    }
}
