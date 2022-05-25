using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddScope : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "DiscordWebhooks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Scope",
                table: "DiscordWebhooks");
        }
    }
}
