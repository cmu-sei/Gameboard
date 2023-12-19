using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class CreateChallengeSubmissionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingSubmission",
                table: "Challenges",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChallengeSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubmittedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Answers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChallengeId = table.Column<string>(type: "nvarchar(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_ChallengeId",
                table: "ChallengeSubmissions",
                column: "ChallengeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeSubmissions");

            migrationBuilder.DropColumn(
                name: "PendingSubmission",
                table: "Challenges");
        }
    }
}
