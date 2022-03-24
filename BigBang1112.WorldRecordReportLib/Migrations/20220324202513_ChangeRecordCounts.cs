using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class ChangeRecordCounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecordCounts_Maps_MapId",
                table: "RecordCounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecordCounts",
                table: "RecordCounts");

            migrationBuilder.RenameTable(
                name: "RecordCounts",
                newName: "RecordCounts2");

            migrationBuilder.RenameIndex(
                name: "IX_RecordCounts_MapId",
                table: "RecordCounts2",
                newName: "IX_RecordCounts2_MapId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecordCounts2",
                table: "RecordCounts2",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecordCounts2_Maps_MapId",
                table: "RecordCounts2",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecordCounts2_Maps_MapId",
                table: "RecordCounts2");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RecordCounts2",
                table: "RecordCounts2");

            migrationBuilder.RenameTable(
                name: "RecordCounts2",
                newName: "RecordCounts");

            migrationBuilder.RenameIndex(
                name: "IX_RecordCounts2_MapId",
                table: "RecordCounts",
                newName: "IX_RecordCounts_MapId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RecordCounts",
                table: "RecordCounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RecordCounts_Maps_MapId",
                table: "RecordCounts",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
