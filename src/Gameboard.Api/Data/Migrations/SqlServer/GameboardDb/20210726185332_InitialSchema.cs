using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Competition = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Season = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Track = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Division = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sponsor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Background = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TestCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    GameStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GameEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistrationMarkdown = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RegistrationOpen = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistrationClose = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RegistrationType = table.Column<int>(type: "int", nullable: false),
                    RegistrationConstraint = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    MinTeamSize = table.Column<int>(type: "int", nullable: false),
                    MaxTeamSize = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    SessionMinutes = table.Column<int>(type: "int", nullable: false),
                    SessionLimit = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    RequireSponsoredTeam = table.Column<bool>(type: "bit", nullable: false),
                    AllowPreview = table.Column<bool>(type: "bit", nullable: false),
                    AllowReset = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Logo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Approved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ApprovedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sponsor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeSpecs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GameId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    AverageDeploySeconds = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    R = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSpecs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GameId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ApprovedName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Sponsor = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    InviteCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Role = table.Column<int>(type: "int", nullable: false),
                    SessionBegin = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SessionEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SessionMinutes = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Time = table.Column<long>(type: "bigint", nullable: false),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    PartialCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Players_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ExternalId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PlayerId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TeamId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    GameId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    LastScoreTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WhenCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    HasDeployedGamespace = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Challenges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChallengeId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    TeamId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeEvents_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeEvents_ChallengeId",
                table: "ChallengeEvents",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_GameId",
                table: "Challenges",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_PlayerId",
                table: "Challenges",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_TeamId",
                table: "Challenges",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSpecs_GameId",
                table: "ChallengeSpecs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameId",
                table: "Players",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeEvents");

            migrationBuilder.DropTable(
                name: "ChallengeSpecs");

            migrationBuilder.DropTable(
                name: "Sponsors");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
