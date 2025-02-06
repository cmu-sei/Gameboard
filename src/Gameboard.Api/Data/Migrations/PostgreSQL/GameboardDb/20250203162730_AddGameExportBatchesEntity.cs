using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddGameExportBatchesEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameExportBatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ExportedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExportedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameExportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameExportBatches_Users_ExportedByUserId",
                        column: x => x.ExportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GameGameExportBatch",
                columns: table => new
                {
                    ExportedInBatchesId = table.Column<string>(type: "text", nullable: false),
                    IncludedGamesId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGameExportBatch", x => new { x.ExportedInBatchesId, x.IncludedGamesId });
                    table.ForeignKey(
                        name: "FK_GameGameExportBatch_GameExportBatches_ExportedInBatchesId",
                        column: x => x.ExportedInBatchesId,
                        principalTable: "GameExportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGameExportBatch_Games_IncludedGamesId",
                        column: x => x.IncludedGamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameExportBatches_ExportedByUserId",
                table: "GameExportBatches",
                column: "ExportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGameExportBatch_IncludedGamesId",
                table: "GameGameExportBatch",
                column: "IncludedGamesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameGameExportBatch");

            migrationBuilder.DropTable(
                name: "GameExportBatches");
        }
    }
}
