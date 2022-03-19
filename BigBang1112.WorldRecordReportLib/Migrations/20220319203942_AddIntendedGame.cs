using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddIntendedGame : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IntendedGameId",
                table: "Maps",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[] { 3, "Trackmania Nations Forever", "TMNF" });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[] { 4, "Trackmania United", "TMU" });

            migrationBuilder.CreateIndex(
                name: "IX_Maps_IntendedGameId",
                table: "Maps",
                column: "IntendedGameId");

            migrationBuilder.AddForeignKey(
                name: "FK_Maps_Games_IntendedGameId",
                table: "Maps",
                column: "IntendedGameId",
                principalTable: "Games",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Maps_Games_IntendedGameId",
                table: "Maps");

            migrationBuilder.DropIndex(
                name: "IX_Maps_IntendedGameId",
                table: "Maps");

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DropColumn(
                name: "IntendedGameId",
                table: "Maps");
        }
    }
}
