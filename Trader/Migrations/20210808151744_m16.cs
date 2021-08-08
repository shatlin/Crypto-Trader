using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m16 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaitTillCancellingOrderTime",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isBuyCostAccurated",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isSellAmountAccurated",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "WaitTillCancellingOrderTime",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "isBuyCostAccurated",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "isSellAmountAccurated",
                table: "Player");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WaitTillCancellingOrderTime",
                table: "PlayerTrades",
                type: "datetime2",
                nullable: true);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "WaitTillCancellingOrderTime",
                table: "Player",
                type: "datetime2",
                nullable: true);

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
    }
}
