using Microsoft.EntityFrameworkCore.Migrations;

namespace Trader2.Migrations
{
    public partial class m3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Config",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxPauses = table.Column<int>(type: "int", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalConsecutiveLosses = table.Column<int>(type: "int", nullable: false),
                    TotalCurrentPauses = table.Column<int>(type: "int", nullable: false),
                    MaxConsecutiveLossesBeforePause = table.Column<int>(type: "int", nullable: false),
                    Botname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsProd = table.Column<bool>(type: "bit", nullable: false),
                    MinimumAmountToTradeWith = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BufferPriceForBuyAndSell = table.Column<decimal>(type: "decimal(18,12)", nullable: false),
                    CommisionAmount = table.Column<decimal>(type: "decimal(18,12)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "Config",
                columns: new[] { "id", "Botname", "BufferPriceForBuyAndSell", "CommisionAmount", "IntervalMinutes", "IsProd", "MaxConsecutiveLossesBeforePause", "MaxPauses", "MinimumAmountToTradeWith", "TotalConsecutiveLosses", "TotalCurrentPauses" },
                values: new object[] { 1, "DIANA", 0.075m, 0.075m, 15, false, 3, 16, 70m, 0, 0 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Config");
        }
    }
}
