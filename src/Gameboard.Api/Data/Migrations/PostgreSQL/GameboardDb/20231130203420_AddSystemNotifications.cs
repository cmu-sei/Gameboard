// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MarkdownContent = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotificationInteractions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SawCalloutOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SawFullNotificationOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SystemNotificationId = table.Column<string>(type: "character varying(40)", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotificationInteractions", x => x.Id);
                    table.UniqueConstraint("AK_SystemNotificationInteractions_SystemNotificationId_UserId", x => new { x.SystemNotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SystemNotificationInteractions_SystemNotifications_SystemNo~",
                        column: x => x.SystemNotificationId,
                        principalTable: "SystemNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemNotificationInteractions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationInteractions_UserId",
                table: "SystemNotificationInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_CreatedByUserId",
                table: "SystemNotifications",
                column: "CreatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemNotificationInteractions");

            migrationBuilder.DropTable(
                name: "SystemNotifications");
        }
    }
}
