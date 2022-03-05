using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class UpdateReportType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_ReportTypes_TypeId",
                table: "Reports");

            migrationBuilder.DropTable(
                name: "ReportTypes");

            migrationBuilder.DropIndex(
                name: "IX_Reports_TypeId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "TypeId",
                table: "Reports",
                newName: "Type");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Reports",
                newName: "TypeId");

            migrationBuilder.CreateTable(
                name: "ReportTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTypes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_TypeId",
                table: "Reports",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_ReportTypes_TypeId",
                table: "Reports",
                column: "TypeId",
                principalTable: "ReportTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
