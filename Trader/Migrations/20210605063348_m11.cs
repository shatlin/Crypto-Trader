using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader.Migrations
{
    public partial class m11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CandleLastUpdatedTime",
                table: "Counter",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DailyCandleLastUpdatedTime",
                table: "Counter",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsCandleCurrentlyBeingUpdated",
                table: "Counter",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDailyCandleCurrentlyBeingUpdated",
                table: "Counter",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CandleLastUpdatedTime",
                table: "Counter");

            migrationBuilder.DropColumn(
                name: "DailyCandleLastUpdatedTime",
                table: "Counter");

            migrationBuilder.DropColumn(
                name: "IsCandleCurrentlyBeingUpdated",
                table: "Counter");

            migrationBuilder.DropColumn(
                name: "IsDailyCandleCurrentlyBeingUpdated",
                table: "Counter");
        }
    }
}
