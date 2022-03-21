using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BigBang1112.WorldRecordReportLib.Migrations
{
    public partial class AddGameToMapGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapGroups_TitlePacks_TitlePackId",
                table: "MapGroups");

            migrationBuilder.AlterColumn<int>(
                name: "TitlePackId",
                table: "MapGroups",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "GameId",
                table: "MapGroups",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MapGroups_GameId",
                table: "MapGroups",
                column: "GameId");

            migrationBuilder.AddForeignKey(
                name: "FK_MapGroups_Games_GameId",
                table: "MapGroups",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MapGroups_TitlePacks_TitlePackId",
                table: "MapGroups",
                column: "TitlePackId",
                principalTable: "TitlePacks",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapGroups_Games_GameId",
                table: "MapGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_MapGroups_TitlePacks_TitlePackId",
                table: "MapGroups");

            migrationBuilder.DropIndex(
                name: "IX_MapGroups_GameId",
                table: "MapGroups");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "MapGroups");

            migrationBuilder.AlterColumn<int>(
                name: "TitlePackId",
                table: "MapGroups",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MapGroups_TitlePacks_TitlePackId",
                table: "MapGroups",
                column: "TitlePackId",
                principalTable: "TitlePacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
