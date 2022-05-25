using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddLastNicknameChangeOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNicknameChangeOn",
                table: "Logins",
                type: "datetime",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastNicknameChangeOn",
                table: "Logins");
        }
    }
}
