using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m37 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ScalpFifteenMinDiffLessThan",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScalpFifteenMinDownMoreThan",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ScalpFiveMinDiffLessThan",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScalpFiveMinDownMoreThan",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ScalpFourHourDiffLessThan",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScalpFourHourDownMoreThan",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ScalpOneHourDiffLessThan",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScalpOneHourDownMoreThan",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ScalpThirtyMinDiffLessThan",
                table: "Config",
                type: "decimal(6,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ScalpThirtyMinDownMoreThan",
                table: "Config",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScalpFifteenMinDiffLessThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpFifteenMinDownMoreThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpFiveMinDiffLessThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpFiveMinDownMoreThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpFourHourDiffLessThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpFourHourDownMoreThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpOneHourDiffLessThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpOneHourDownMoreThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpThirtyMinDiffLessThan",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "ScalpThirtyMinDownMoreThan",
                table: "Config");
        }
    }
}
