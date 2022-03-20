using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddMoreGames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[] { 5, "Trackmania Sunrise", "TMS" });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[] { 6, "Trackmania Nations ESWC", "TMN" });

            migrationBuilder.InsertData(
                table: "Games",
                columns: new[] { "Id", "DisplayName", "Name" },
                values: new object[] { 7, "Trackmania Original", "TMO" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Games",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
