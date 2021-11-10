using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class ChallengeGates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrerequisiteId",
                table: "ChallengeSpecs");

            migrationBuilder.DropColumn(
                name: "PrerequisiteScore",
                table: "ChallengeSpecs");

            migrationBuilder.CreateTable(
                name: "ChallengeGates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TargetId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequiredId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequiredScore = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeGates_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeGates_GameId",
                table: "ChallengeGates",
                column: "GameId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeGates");

            migrationBuilder.AddColumn<string>(
                name: "PrerequisiteId",
                table: "ChallengeSpecs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrerequisiteScore",
                table: "ChallengeSpecs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
