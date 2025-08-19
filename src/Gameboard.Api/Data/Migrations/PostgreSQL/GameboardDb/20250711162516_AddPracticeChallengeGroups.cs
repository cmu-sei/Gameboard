using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPracticeChallengeGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PracticeChallengeGroups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParentGroupId = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeChallengeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_PracticeChallengeGroups_ParentGroup~",
                        column: x => x.ParentGroupId,
                        principalTable: "PracticeChallengeGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_CreatedByUserId",
                table: "PracticeChallengeGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_ParentGroupId",
                table: "PracticeChallengeGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_UpdatedByUserId",
                table: "PracticeChallengeGroups",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChallengeSpecPracticeChallengeGroup");

            migrationBuilder.DropTable(
                name: "PracticeChallengeGroups");
        }
    }
}
