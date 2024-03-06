using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class MigrateDummyChallengesToAdvanceScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // previous versions of gameboard created a dummy challenge when a player advances
            // from one game to another that holds the points they earned in the previous
            // game. We now have a dedicated "AdvancedWithScore" column on Players that 
            // represents this value, so record the sum of all dummy challenges for each team
            // and update their AdvancedWithScore to the correct value. Then dump the
            // dummy challenges.
            migrationBuilder
                .Sql(""" 
                    CREATE TEMP TABLE InitialScoreChallenges AS
                    SELECT c."TeamId", SUM(c."Score") AS "Score"
                    FROM "Challenges" c
                    INNER JOIN "Players" p On p."TeamId" = c."TeamId"
                    WHERE c."SpecId" = '_initialscore_'
                    GROUP BY c."TeamId";

                    UPDATE "Players" AS p
                    SET "AdvancedWithScore" = c."Score"
                    FROM InitialScoreChallenges AS c
                    WHERE c."TeamId" = p."TeamId";
                        
                    DELETE FROM "Challenges" WHERE "SpecId" = '_initialscore_';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // create the dummy challenges again, revert "AdvancedWithScore" to NULL.
            // Have to make some inferences about fields like start/end time.
            migrationBuilder
            .Sql("""
                    INSERT INTO "Challenges"
                    (
                        "Id",
                        "PlayerId",
                        "TeamId",
                        "GameId",
                        "SpecId",
                        "Name",
                        "Points",
                        "Score",
                        "HasDeployedGamespace",
                        "StartTime",
                        "EndTime",
                        "LastScoreTime",
                        "LastSyncTime",
                        "WhenCreated"
                    )
                    SELECT
                        gen_random_uuid(),
                        p."Id",
                        p."TeamId",
                        p."GameId",
                        '_initialscore_',
                        '_initialscore_',
                        p."AdvancedWithScore",
                        p."AdvancedWithScore",
                        FALSE,
                        p."SessionBegin",
                        NOW(),
                        p."SessionEnd",
                        p."SessionBegin",
                        p."SessionBegin"
                    FROM "Players" p
                    WHERE p."AdvancedWithScore" IS NOT NULL;

                    UPDATE "Players" SET "AdvancedWithScore" = NULL 
                    WHERE "AdvancedWithScore" IS NOT NULL;
                """);
        }
    }
}
