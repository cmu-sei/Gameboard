using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class PlayerAdvanceFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Advanced",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrerequisiteId",
                table: "ChallengeSpecs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrerequisiteScore",
                table: "ChallengeSpecs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Advanced",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PrerequisiteId",
                table: "ChallengeSpecs");

            migrationBuilder.DropColumn(
                name: "PrerequisiteScore",
                table: "ChallengeSpecs");
        }
    }
}
