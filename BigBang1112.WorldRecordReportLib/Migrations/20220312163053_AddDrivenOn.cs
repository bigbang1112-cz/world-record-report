using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddDrivenOn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DrivenOn",
                table: "RecordSetDetailedChanges",
                type: "datetime",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Maps",
                keyColumn: "DeformattedName",
                keyValue: null,
                column: "DeformattedName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "DeformattedName",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DrivenOn",
                table: "RecordSetDetailedChanges");

            migrationBuilder.AlterColumn<string>(
                name: "DeformattedName",
                table: "Maps",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
