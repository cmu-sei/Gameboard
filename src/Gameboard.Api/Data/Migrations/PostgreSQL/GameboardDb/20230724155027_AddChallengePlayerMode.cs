using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengePlayerMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerMode",
                table: "Challenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerMode",
                table: "ArchivedChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(""" 
                UPDATE "Challenges" AS c 
                    SET "PlayerMode" = g."PlayerMode" 
                FROM "Games" AS g 
                WHERE g."Id" = c."GameId";
            """);

            migrationBuilder.Sql(""" 
                UPDATE "ArchivedChallenges" AS ac 
                    SET "PlayerMode" = g."PlayerMode" 
                FROM "Games" AS g 
                WHERE g."Id" = ac."GameId";
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerMode",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "PlayerMode",
                table: "ArchivedChallenges");
        }
    }
}
