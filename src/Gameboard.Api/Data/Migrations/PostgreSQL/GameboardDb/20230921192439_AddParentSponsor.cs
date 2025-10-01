// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddParentSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentSponsorId",
                table: "Sponsors",
                type: "character varying(40)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_ParentSponsorId",
                table: "Sponsors",
                column: "ParentSponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sponsors_Sponsors_ParentSponsorId",
                table: "Sponsors",
                column: "ParentSponsorId",
                principalTable: "Sponsors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sponsors_Sponsors_ParentSponsorId",
                table: "Sponsors");

            migrationBuilder.DropIndex(
                name: "IX_Sponsors_ParentSponsorId",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "ParentSponsorId",
                table: "Sponsors");
        }
    }
}
