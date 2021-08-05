using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WaitTillCancellingOrderTime",
                table: "PlayerTrades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isBuyAllowed",
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
                name: "isBuyAllowed",
                table: "Player",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSellingAllowed",
                table: "Config",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaximumAmountForaBot",
                table: "Config",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaitTillCancellingOrderTime",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "isBuyAllowed",
                table: "PlayerTrades");

            migrationBuilder.DropColumn(
                name: "WaitTillCancellingOrderTime",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "isBuyAllowed",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "IsSellingAllowed",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "MaximumAmountForaBot",
                table: "Config");
        }
    }
}
