// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddManualTeamBonuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManualChallengeBonuses_Challenges_ChallengeId",
                table: "ManualChallengeBonuses");

            migrationBuilder.DropForeignKey(
                name: "FK_ManualChallengeBonuses_Users_EnteredByUserId",
                table: "ManualChallengeBonuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ManualChallengeBonuses",
                table: "ManualChallengeBonuses");

            migrationBuilder.RenameTable(
                name: "ManualChallengeBonuses",
                newName: "ManualBonuses");

            migrationBuilder.RenameIndex(
                name: "IX_ManualChallengeBonuses_EnteredByUserId",
                table: "ManualBonuses",
                newName: "IX_ManualBonuses_EnteredByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ManualChallengeBonuses_ChallengeId",
                table: "ManualBonuses",
                newName: "IX_ManualBonuses_ChallengeId");

            migrationBuilder.AddColumn<string>(
                name: "TeamId",
                table: "ManualBonuses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "ManualBonuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManualBonuses",
                table: "ManualBonuses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualBonuses_Challenges_ChallengeId",
                table: "ManualBonuses",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManualBonuses_Users_EnteredByUserId",
                table: "ManualBonuses",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManualBonuses_Challenges_ChallengeId",
                table: "ManualBonuses");

            migrationBuilder.DropForeignKey(
                name: "FK_ManualBonuses_Users_EnteredByUserId",
                table: "ManualBonuses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ManualBonuses",
                table: "ManualBonuses");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "ManualBonuses");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ManualBonuses");

            migrationBuilder.RenameTable(
                name: "ManualBonuses",
                newName: "ManualChallengeBonuses");

            migrationBuilder.RenameIndex(
                name: "IX_ManualBonuses_EnteredByUserId",
                table: "ManualChallengeBonuses",
                newName: "IX_ManualChallengeBonuses_EnteredByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_ManualBonuses_ChallengeId",
                table: "ManualChallengeBonuses",
                newName: "IX_ManualChallengeBonuses_ChallengeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ManualChallengeBonuses",
                table: "ManualChallengeBonuses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualChallengeBonuses_Challenges_ChallengeId",
                table: "ManualChallengeBonuses",
                column: "ChallengeId",
                principalTable: "Challenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ManualChallengeBonuses_Users_EnteredByUserId",
                table: "ManualChallengeBonuses",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
