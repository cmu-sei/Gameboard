// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System;
using Gameboard.Api.Common.Services;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
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
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CertificateHtmlTemplate = table.Column<string>(type: "text", nullable: true),
                    DefaultPracticeSessionLengthMinutes = table.Column<int>(type: "integer", nullable: false),
                    IntroTextMarkdown = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MaxConcurrentPracticeSessions = table.Column<int>(type: "integer", nullable: true),
                    MaxPracticeSessionLengthMinutes = table.Column<int>(type: "integer", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
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
                unique: true);


            // seed default settings
            var introTextMarkdown = "Welcome to the Practice area. Search for and select any challenge to practice your skills. If you''re a beginner, search for \"Training Labs\" for walkthroughs, and \"Practice Challenge\" for a place to start.";

            migrationBuilder.Sql($"""
                INSERT INTO "PracticeModeSettings" ("Id", "DefaultPracticeSessionLengthMinutes", "IntroTextMarkdown", "MaxPracticeSessionLengthMinutes", "UpdatedOn")
                VALUES ('{GuidService.StaticGenerateGuid()}', 60, '{introTextMarkdown}', 240, NOW());
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
