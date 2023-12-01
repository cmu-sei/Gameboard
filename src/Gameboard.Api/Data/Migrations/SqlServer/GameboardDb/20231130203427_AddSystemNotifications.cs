using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalGameTeam_Games_GameId",
                table: "ExternalGameTeam");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ExternalGameTeam_TeamId_GameId",
                table: "ExternalGameTeam");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalGameTeam",
                table: "ExternalGameTeam");

            migrationBuilder.RenameTable(
                name: "ExternalGameTeam",
                newName: "ExternalGameTeams");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalGameTeam_GameId",
                table: "ExternalGameTeams",
                newName: "IX_ExternalGameTeams_GameId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ExternalGameTeams_TeamId_GameId",
                table: "ExternalGameTeams",
                columns: new[] { "TeamId", "GameId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalGameTeams",
                table: "ExternalGameTeams",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MarkdownContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(40)", nullable: false)
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
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SawCalloutOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SawFullNotificationOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DismissedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SystemNotificationId = table.Column<string>(type: "nvarchar(40)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotificationInteractions", x => x.Id);
                    table.UniqueConstraint("AK_SystemNotificationInteractions_SystemNotificationId_UserId", x => new { x.SystemNotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SystemNotificationInteractions_SystemNotifications_SystemNotificationId",
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

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalGameTeams_Games_GameId",
                table: "ExternalGameTeams",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExternalGameTeams_Games_GameId",
                table: "ExternalGameTeams");

            migrationBuilder.DropTable(
                name: "SystemNotificationInteractions");

            migrationBuilder.DropTable(
                name: "SystemNotifications");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_ExternalGameTeams_TeamId_GameId",
                table: "ExternalGameTeams");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExternalGameTeams",
                table: "ExternalGameTeams");

            migrationBuilder.RenameTable(
                name: "ExternalGameTeams",
                newName: "ExternalGameTeam");

            migrationBuilder.RenameIndex(
                name: "IX_ExternalGameTeams_GameId",
                table: "ExternalGameTeam",
                newName: "IX_ExternalGameTeam_GameId");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_ExternalGameTeam_TeamId_GameId",
                table: "ExternalGameTeam",
                columns: new[] { "TeamId", "GameId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExternalGameTeam",
                table: "ExternalGameTeam",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExternalGameTeam_Games_GameId",
                table: "ExternalGameTeam",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
