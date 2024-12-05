using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class CreateFeedbackTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GameChallengesFeedbackTemplateId",
                table: "Games",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameFeedbackTemplateId",
                table: "Games",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeedbackTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_GameChallengesFeedbackTemplateId",
                table: "Games",
                column: "GameChallengesFeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_GameFeedbackTemplateId",
                table: "Games",
                column: "GameFeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackTemplates_CreatedByUserId",
                table: "FeedbackTemplates",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_GameChallengesFeedbackTemplateId",
                table: "Games",
                column: "GameChallengesFeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_GameFeedbackTemplateId",
                table: "Games",
                column: "GameFeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_GameChallengesFeedbackTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_GameFeedbackTemplateId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "FeedbackTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Games_GameChallengesFeedbackTemplateId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_GameFeedbackTemplateId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameChallengesFeedbackTemplateId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameFeedbackTemplateId",
                table: "Games");
        }
    }
}
