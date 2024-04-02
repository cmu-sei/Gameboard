using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddExternalGameHosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalGameClientUrl",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExternalGameStartupEndpoint",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExternalGameTeamExtendedEndpoint",
                table: "Games");

            migrationBuilder.CreateTable(
                name: "ExternalGameHosts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DestroyResourcesOnDeployFailure = table.Column<bool>(type: "boolean", nullable: false),
                    GamespaceDeployBatchSize = table.Column<int>(type: "integer", nullable: true),
                    HostApiKey = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    HostUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PingEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartupEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamExtendedEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalGameHosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalGameHosts_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalGameHosts_GameId",
                table: "ExternalGameHosts",
                column: "GameId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalGameHosts");

            migrationBuilder.AddColumn<string>(
                name: "ExternalGameClientUrl",
                table: "Games",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalGameStartupEndpoint",
                table: "Games",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalGameTeamExtendedEndpoint",
                table: "Games",
                type: "text",
                nullable: true);
        }
    }
}
