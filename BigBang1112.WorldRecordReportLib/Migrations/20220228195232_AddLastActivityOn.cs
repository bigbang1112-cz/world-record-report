using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddLastActivityOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityOn",
                table: "Maps",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastActivityOn",
                table: "Maps");
        }
    }
}
