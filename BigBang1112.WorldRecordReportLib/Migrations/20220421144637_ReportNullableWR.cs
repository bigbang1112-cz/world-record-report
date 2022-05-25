using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class ReportNullableWR : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_WorldRecords_WorldRecordId",
                table: "Reports");

            migrationBuilder.AlterColumn<int>(
                name: "WorldRecordId",
                table: "Reports",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_WorldRecords_WorldRecordId",
                table: "Reports",
                column: "WorldRecordId",
                principalTable: "WorldRecords",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_WorldRecords_WorldRecordId",
                table: "Reports");

            migrationBuilder.AlterColumn<int>(
                name: "WorldRecordId",
                table: "Reports",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_WorldRecords_WorldRecordId",
                table: "Reports",
                column: "WorldRecordId",
                principalTable: "WorldRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
