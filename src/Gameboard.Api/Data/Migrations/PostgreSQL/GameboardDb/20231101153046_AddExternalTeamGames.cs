using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddExternalTeamGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalGameTeams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalGameUrl = table.Column<string>(type: "text", nullable: true),
                    DeployStatus = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalGameTeams", x => x.Id);
                    table.UniqueConstraint("AK_ExternalGameTeams_TeamId_GameId", x => new { x.TeamId, x.GameId });
                    table.ForeignKey(
                        name: "FK_ExternalGameTeams_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalGameTeams_GameId",
                table: "ExternalGameTeams",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalGameTeams");
        }
    }
}
