// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class UpdateChallengeSpecModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateHtmlTemplate",
                table: "PracticeModeSettings");

            migrationBuilder.DropColumn(
                name: "TestCode",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateHtmlTemplate",
                table: "PracticeModeSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCode",
                table: "Games",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
