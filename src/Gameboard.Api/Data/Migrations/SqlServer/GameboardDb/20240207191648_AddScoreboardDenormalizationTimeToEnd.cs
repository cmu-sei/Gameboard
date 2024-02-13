using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddScoreboardDenormalizationTimeToEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeRemainingMs",
                table: "DenormalizedTeamScores",
                newName: "TimeToSessionEndMs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeToSessionEndMs",
                table: "DenormalizedTeamScores",
                newName: "TimeRemainingMs");
        }
    }
}
