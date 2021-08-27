using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m36 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(name: "PK_SignalCandle",table: "SignalCandle");

            migrationBuilder.DropColumn(
                    name: "Id",
                    table: "SignalCandle");


            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "SignalCandle",
                type: "uniqueidentifier",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AlterColumn<long>(
            //    name: "Id",
            //    table: "SignalCandle",
            //    type: "bigint",
            //    nullable: false,
            //    oldClrType: typeof(Guid),
            //    oldType: "uniqueidentifier")
            //    .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
