using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class ModifyExternalGameHostsAsOneToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalGameHosts_Games_GameId",
                table: "ExternalGameHosts");

            migrationBuilder.DropIndex(
                name: "IX_ExternalGameHosts_GameId",
                table: "ExternalGameHosts");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "ExternalGameHosts");

            migrationBuilder.AddColumn<string>(
                name: "ExternalHostId",
                table: "Games",
                type: "character varying(40)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_ExternalHostId",
                table: "Games",
                column: "ExternalHostId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_ExternalGameHosts_ExternalHostId",
                table: "Games",
                column: "ExternalHostId",
                principalTable: "ExternalGameHosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_ExternalGameHosts_ExternalHostId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_ExternalHostId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExternalHostId",
                table: "Games");

            migrationBuilder.AddColumn<string>(
                name: "GameId",
                table: "ExternalGameHosts",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalGameHosts_GameId",
                table: "ExternalGameHosts",
                column: "GameId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalGameHosts_Games_GameId",
                table: "ExternalGameHosts",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
