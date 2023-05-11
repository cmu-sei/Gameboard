﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddAutoChallengeBonusSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChallengeBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PointValue = table.Column<double>(type: "double precision", nullable: false),
                    ChallengeBonusType = table.Column<int>(type: "integer", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", nullable: true),
                    SolveRank = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeBonuses_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwardedChallengeBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EnteredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    InternalSummary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChallengeBonusId = table.Column<string>(type: "character varying(40)", nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardedChallengeBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardedChallengeBonuses_ChallengeBonuses_ChallengeBonusId",
                        column: x => x.ChallengeBonusId,
                        principalTable: "ChallengeBonuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardedChallengeBonuses_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AwardedChallengeBonuses_ChallengeBonusId",
                table: "AwardedChallengeBonuses",
                column: "ChallengeBonusId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardedChallengeBonuses_ChallengeId",
                table: "AwardedChallengeBonuses",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeBonuses_ChallengeSpecId",
                table: "ChallengeBonuses",
                column: "ChallengeSpecId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AwardedChallengeBonuses");

            migrationBuilder.DropTable(
                name: "ChallengeBonuses");
        }
    }
}
