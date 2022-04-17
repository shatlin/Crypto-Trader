using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m54 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          

            migrationBuilder.RenameColumn(
                name: "DontSellBelowPerc",
                table: "PlayerTrades",
                newName: "LossSellBelow");

            migrationBuilder.RenameColumn(
                name: "DontSellBelowPerc",
                table: "Player",
                newName: "LossSellBelow");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LossSellBelow",
                table: "PlayerTrades",
                newName: "DontSellBelowPerc");

            migrationBuilder.RenameColumn(
                name: "LossSellBelow",
                table: "Player",
                newName: "DontSellBelowPerc");

        }
    }
}
