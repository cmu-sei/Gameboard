// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPracticeChallengeGroupChallengeSpecJoinEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeSpecPracticeChallengeGroup");

            migrationBuilder.CreateTable(
                name: "PracticeChallengeGroupChallengeSpec",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PracticeChallengeGroupId = table.Column<string>(type: "text", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeChallengeGroupChallengeSpec", x => x.Id);
                    table.UniqueConstraint("AK_PracticeChallengeGroupChallengeSpec_ChallengeSpecId_Practic~", x => new { x.ChallengeSpecId, x.PracticeChallengeGroupId });
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroupChallengeSpec_ChallengeSpecs_Challeng~",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroupChallengeSpec_PracticeChallengeGroups~",
                        column: x => x.PracticeChallengeGroupId,
                        principalTable: "PracticeChallengeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroupChallengeSpec_PracticeChallengeGroupId",
                table: "PracticeChallengeGroupChallengeSpec",
                column: "PracticeChallengeGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PracticeChallengeGroupChallengeSpec");

            migrationBuilder.CreateTable(
                name: "ChallengeSpecPracticeChallengeGroup",
                columns: table => new
                {
                    ChallengeSpecsId = table.Column<string>(type: "character varying(40)", nullable: false),
                    PracticeChallengeGroupsId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSpecPracticeChallengeGroup", x => new { x.ChallengeSpecsId, x.PracticeChallengeGroupsId });
                    table.ForeignKey(
                        name: "FK_ChallengeSpecPracticeChallengeGroup_ChallengeSpecs_Challeng~",
                        column: x => x.ChallengeSpecsId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChallengeSpecPracticeChallengeGroup_PracticeChallengeGroups~",
                        column: x => x.PracticeChallengeGroupsId,
                        principalTable: "PracticeChallengeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSpecPracticeChallengeGroup_PracticeChallengeGroups~",
                table: "ChallengeSpecPracticeChallengeGroup",
                column: "PracticeChallengeGroupsId");
        }
    }
}
