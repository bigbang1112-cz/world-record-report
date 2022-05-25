using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddLastRefreshedOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LastRefreshedOn",
                table: "Maps",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRefreshedOn",
                table: "Maps");
        }
    }
}
