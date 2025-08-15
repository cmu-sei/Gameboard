using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengeSpecFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Challenges_SpecId",
                table: "Challenges",
                column: "SpecId");

            // because this was previously not a proper FK relationship, there could be orphaned specIds in the challenges table, so
            // we have to manually insert ChallengeSpec records for any such orphaned challenges.
            migrationBuilder.Sql
            (
                """
                INSERT INTO "ChallengeSpecs"
                (
                    "Id",
                    "AverageDeploySeconds",
                    "Disabled",
                    "GameEngineType",
                    "GameId",
                    "IsHidden",
                    "Points",
                    "ShowSolutionGuideInCompetitiveMode",
                    "R",
                    "X",
                    "Y"
                )
                SELECT
                    "SpecId",
                    0,
                    TRUE,
                    0,
                    MAX("GameId"),
                    FALSE,
                    MAX("Points"),
                    FALSE,
                    0.015,
                    0,
                    0
                FROM "Challenges" WHERE "SpecId" NOT IN(SELECT "Id" FROM "ChallengeSpecs")
                GROUP BY "SpecId";
                """
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_ChallengeSpecs_SpecId",
                table: "Challenges",
                column: "SpecId",
                principalTable: "ChallengeSpecs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_ChallengeSpecs_SpecId",
                table: "Challenges");

            migrationBuilder.DropIndex(
                name: "IX_Challenges_SpecId",
                table: "Challenges");
        }
    }
}
