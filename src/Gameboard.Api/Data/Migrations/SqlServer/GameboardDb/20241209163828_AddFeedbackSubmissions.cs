using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddFeedbackSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttachedEntityType = table.Column<int>(type: "int", nullable: false),
                    WhenEdited = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    WhenSubmitted = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TeamId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeedbackTemplateId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "nvarchar(40)", nullable: true),
                    GameId = table.Column<string>(type: "nvarchar(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_FeedbackTemplates_FeedbackTemplateId",
                        column: x => x.FeedbackTemplateId,
                        principalTable: "FeedbackTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackSubmissionResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FeedbackSubmissionId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSubmissionResponses", x => new { x.FeedbackSubmissionId, x.Id });
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissionResponses_FeedbackSubmissions_FeedbackSubmissionId",
                        column: x => x.FeedbackSubmissionId,
                        principalTable: "FeedbackSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_ChallengeSpecId",
                table: "FeedbackSubmissions",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_FeedbackTemplateId",
                table: "FeedbackSubmissions",
                column: "FeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_GameId",
                table: "FeedbackSubmissions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_UserId",
                table: "FeedbackSubmissions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackSubmissionResponses");

            migrationBuilder.DropTable(
                name: "FeedbackSubmissions");
        }
    }
}
