using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class Feedback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedbackConfig",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Answers = table.Column<string>(type: "character varying(8192)", maxLength: 8192, nullable: true),
                    Submitted = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ChallengeId",
                table: "Feedback",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ChallengeSpecId",
                table: "Feedback",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_GameId",
                table: "Feedback",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_PlayerId",
                table: "Feedback",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_UserId",
                table: "Feedback",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropColumn(
                name: "FeedbackConfig",
                table: "Games");
        }
    }
}
