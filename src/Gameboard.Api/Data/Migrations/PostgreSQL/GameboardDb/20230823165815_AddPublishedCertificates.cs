using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPublishedCertificates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PublishedCertificate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PublishedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(40)", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedCertificate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerUserId_Users_Id",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PublishedCertificate_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PublishedCertificate_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_ChallengeSpecId",
                table: "PublishedCertificate",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_GameId",
                table: "PublishedCertificate",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_OwnerUserId",
                table: "PublishedCertificate",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishedCertificate");
        }
    }
}
