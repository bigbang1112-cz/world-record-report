using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddLoginAccountIdAndIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "IX_Logins_Name",
                table: "Logins",
                column: "Name");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logins_AccountId",
                table: "Logins");

            migrationBuilder.DropIndex(
                name: "IX_Logins_Name",
                table: "Logins");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Logins");
        }
    }
}
