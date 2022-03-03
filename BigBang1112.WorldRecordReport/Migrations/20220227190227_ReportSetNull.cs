using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReport.Migrations
{
    public partial class ReportSetNull : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordWebhookMessages_Reports_ReportId",
                table: "DiscordWebhookMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordWebhookMessages_Reports_ReportId",
                table: "DiscordWebhookMessages",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordWebhookMessages_Reports_ReportId",
                table: "DiscordWebhookMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordWebhookMessages_Reports_ReportId",
                table: "DiscordWebhookMessages",
                column: "ReportId",
                principalTable: "Reports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
