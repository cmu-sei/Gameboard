// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class ArchivedChallenge : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchivedChallenges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GameName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastScoreTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasGamespaceDeployed = table.Column<bool>(type: "boolean", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<string>(type: "text", nullable: true),
                    Submissions = table.Column<string>(type: "text", nullable: true),
                    TeamMembers = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_GameId",
                table: "ArchivedChallenges",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_PlayerId",
                table: "ArchivedChallenges",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_TeamId",
                table: "ArchivedChallenges",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_UserId",
                table: "ArchivedChallenges",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchivedChallenges");
        }
    }
}
