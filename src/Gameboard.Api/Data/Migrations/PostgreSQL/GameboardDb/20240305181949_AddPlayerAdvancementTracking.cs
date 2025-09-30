// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPlayerAdvancementTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromGameId",
                table: "Players",
                type: "character varying(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromPlayerId",
                table: "Players",
                type: "character varying(40)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvancedFromTeamId",
                table: "Players",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AdvancedWithScore",
                table: "Players",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromGameId",
                table: "Players",
                column: "AdvancedFromGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromPlayerId",
                table: "Players",
                column: "AdvancedFromPlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Games_AdvancedFromGameId",
                table: "Players",
                column: "AdvancedFromGameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_Players_AdvancedFromPlayerId",
                table: "Players",
                column: "AdvancedFromPlayerId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Games_AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Players_Players_AdvancedFromPlayerId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Players_AdvancedFromPlayerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromGameId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromPlayerId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedFromTeamId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "AdvancedWithScore",
                table: "Players");
        }
    }
}
