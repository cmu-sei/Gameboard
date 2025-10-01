// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddScoreboardDenormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DenormalizedTeamScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: true),
                    ScoreOverall = table.Column<double>(type: "double precision", nullable: false),
                    ScoreAutoBonus = table.Column<double>(type: "double precision", nullable: false),
                    ScoreManualBonus = table.Column<double>(type: "double precision", nullable: false),
                    ScoreChallenge = table.Column<double>(type: "double precision", nullable: false),
                    SolveCountNone = table.Column<int>(type: "integer", nullable: false),
                    SolveCountPartial = table.Column<int>(type: "integer", nullable: false),
                    SolveCountComplete = table.Column<int>(type: "integer", nullable: false),
                    CumulativeTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    TimeRemainingMs = table.Column<double>(type: "double precision", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenormalizedTeamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenormalizedTeamScores_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DenormalizedTeamScores_GameId",
                table: "DenormalizedTeamScores",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DenormalizedTeamScores");
        }
    }
}
