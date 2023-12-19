using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengeSubmissionScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Score",
                table: "ChallengeSubmissions",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("""UPDATE "ChallengeSubmissions" SET "Score" = 0 WHERE "Score" IS NULL;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "ChallengeSubmissions");
        }
    }
}
