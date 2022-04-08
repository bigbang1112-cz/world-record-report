using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddMapId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MapId",
                table: "Maps",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Maps");
        }
    }
}
