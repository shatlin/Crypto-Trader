using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m26 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeqNo",
                table: "SignalCandle");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SeqNo",
                table: "SignalCandle",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
