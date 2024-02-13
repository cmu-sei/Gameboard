using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddScoreboardDenormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DenormalizedTeamScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TeamName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoreOverall = table.Column<double>(type: "float", nullable: false),
                    ScoreAutoBonus = table.Column<double>(type: "float", nullable: false),
                    ScoreManualBonus = table.Column<double>(type: "float", nullable: false),
                    ScoreChallenge = table.Column<double>(type: "float", nullable: false),
                    SolveCountNone = table.Column<int>(type: "int", nullable: false),
                    SolveCountPartial = table.Column<int>(type: "int", nullable: false),
                    SolveCountComplete = table.Column<int>(type: "int", nullable: false),
                    CumulativeTimeMs = table.Column<double>(type: "float", nullable: false),
                    TimeRemainingMs = table.Column<double>(type: "float", nullable: true),
                    GameId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenormalizedTeamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenormalizedTeamScores_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DenormalizedTeamScores_GameId",
                table: "DenormalizedTeamScores",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DenormalizedTeamScores");
        }
    }
}
