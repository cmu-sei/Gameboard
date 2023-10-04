using System;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPracticeModeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeModeSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CertificateHtmlTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultPracticeSessionLengthMinutes = table.Column<int>(type: "int", nullable: false),
                    IntroTextMarkdown = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MaxConcurrentPracticeSessions = table.Column<int>(type: "int", nullable: true),
                    MaxPracticeSessionLengthMinutes = table.Column<int>(type: "int", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeModeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeModeSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeModeSettings_UpdatedByUserId",
                table: "PracticeModeSettings",
                column: "UpdatedByUserId",
                unique: true,
                filter: "[UpdatedByUserId] IS NOT NULL");

            // seed default settings
            var introTextMarkdown = "Welcome to the Practice area. Search for and select any challenge to practice your skills. If you''re a beginner, search for \"Training Labs\" for walkthroughs, and \"Practice Challenge\" for a place to start.";

            migrationBuilder.Sql($"""
                INSERT INTO PracticeModeSettings (Id, DefaultPracticeSessionLengthMinutes, IntroTextMarkdown, MaxPracticeSessionLengthMinutes, UpdatedOn)
                VALUES ('{GuidService.StaticGenerateGuid()}', 60, '{introTextMarkdown}', 240, GETUTCDATE());
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeModeSettings");
        }
    }
}
