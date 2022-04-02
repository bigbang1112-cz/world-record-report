using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddMapProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logins_AccountId",
                table: "Logins");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Logins");

            migrationBuilder.AddColumn<Guid>(
                name: "DownloadGuid",
                table: "Maps",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MapStyle",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MapType",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadGuid",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "MapStyle",
                table: "Maps");

            migrationBuilder.DropColumn(
                name: "MapType",
                table: "Maps");

            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                table: "Logins",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Logins_AccountId",
                table: "Logins",
                column: "AccountId");
        }
    }
}
