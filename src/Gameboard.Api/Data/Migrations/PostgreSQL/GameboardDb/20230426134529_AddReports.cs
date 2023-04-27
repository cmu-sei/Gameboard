using Gameboard.Api.Features.Reports;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExampleFields = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExampleParameters = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.InsertReport(new Report
            {
                Key = ReportKey.ChallengesReport,
                Name = "Challenges Report",
                Description = "Understand the role a challenge played in its games and competitions, how attainable a full solve was, and more.",
                ExampleFields = "Scores|Solve Times|Deploy vs. Solve Counts",
                ExampleParameters = "Session Date Range|Competition|Track|Game|Challenge"
            });

            migrationBuilder.InsertReport(new Report
            {
                Key = ReportKey.PlayersReport,
                Name = "Players Report",
                Description = "View a player-based perspective of your games and challenge. See who's scoring highly, logging in regularly, and more.",
                ExampleFields = "Scores|Solve Times|Participation Across Games",
                ExampleParameters = "Session Date Range|Competition|Track|Game|Challenge"
            });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reports");
        }
    }
}
