// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class UpdateFeedbackFkNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_GameChallengesFeedbackTemplateId",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "GameChallengesFeedbackTemplateId",
                table: "Games",
                newName: "ChallengesFeedbackTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_Games_GameChallengesFeedbackTemplateId",
                table: "Games",
                newName: "IX_Games_ChallengesFeedbackTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_ChallengesFeedbackTemplateId",
                table: "Games",
                column: "ChallengesFeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_ChallengesFeedbackTemplateId",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "ChallengesFeedbackTemplateId",
                table: "Games",
                newName: "GameChallengesFeedbackTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_Games_ChallengesFeedbackTemplateId",
                table: "Games",
                newName: "IX_Games_GameChallengesFeedbackTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_GameChallengesFeedbackTemplateId",
                table: "Games",
                column: "GameChallengesFeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");
        }
    }
}
