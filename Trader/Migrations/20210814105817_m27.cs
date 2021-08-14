using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m27 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SignalCandle",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<decimal>(
                name: "DayHighGreaterthanToSell",
                table: "Config",
                type: "decimal(18,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayHighLessthanToSell",
                table: "Config",
                type: "decimal(18,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayLowGreaterthanTobuy",
                table: "Config",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DayLowLessthanTobuy",
                table: "Config",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayHighGreaterthanToSell",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "DayHighLessthanToSell",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "DayLowGreaterthanTobuy",
                table: "Config");

            migrationBuilder.DropColumn(
                name: "DayLowLessthanTobuy",
                table: "Config");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "SignalCandle",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");
        }
    }
}
