// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class RenameGameFeedbackTemplateIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_GameFeedbackTemplateId",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "GameFeedbackTemplateId",
                table: "Games",
                newName: "FeedbackTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_Games_GameFeedbackTemplateId",
                table: "Games",
                newName: "IX_Games_FeedbackTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_FeedbackTemplateId",
                table: "Games",
                column: "FeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_FeedbackTemplates_FeedbackTemplateId",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "FeedbackTemplateId",
                table: "Games",
                newName: "GameFeedbackTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_Games_FeedbackTemplateId",
                table: "Games",
                newName: "IX_Games_GameFeedbackTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_FeedbackTemplates_GameFeedbackTemplateId",
                table: "Games",
                column: "GameFeedbackTemplateId",
                principalTable: "FeedbackTemplates",
                principalColumn: "Id");
        }
    }
}
