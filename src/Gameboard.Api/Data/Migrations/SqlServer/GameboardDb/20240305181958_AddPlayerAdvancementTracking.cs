using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPlayerAdvancementTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromGameId",
                table: "Players",
                type: "nvarchar(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromPlayerID",
                table: "Players",
                type: "nvarchar(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromTeamId",
                table: "Players",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AdvancedWithScore",
                table: "Players",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromGameId",
                table: "Players",
                column: "AdvancedFromGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromPlayerID",
                table: "Players",
                column: "AdvancedFromPlayerID");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Games_AdvancedFromGameId",
                table: "Players",
                column: "AdvancedFromGameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Players_AdvancedFromPlayerID",
                table: "Players",
                column: "AdvancedFromPlayerID",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Games_AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Players_AdvancedFromPlayerID",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_AdvancedFromPlayerID",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromPlayerID",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromTeamId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedWithScore",
                table: "Players");
        }
    }
}
