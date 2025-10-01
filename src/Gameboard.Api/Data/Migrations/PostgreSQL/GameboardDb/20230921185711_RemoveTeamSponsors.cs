// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class RemoveTeamSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamSponsors",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamSponsors",
                table: "Players",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            // fall back to just the player's sponsor logo
            migrationBuilder.Sql
            ($"""
                UPDATE "Players" SET "TeamSponsors" = "Sponsors"."Logo"
                FROM "Sponsors"
                WHERE "Sponsors"."Id" = "Players"."SponsorId";
            """);
        }
    }
}
