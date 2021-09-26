using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    public partial class PlayerAdvanceFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Advanced",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrerequisiteId",
                table: "ChallengeSpecs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrerequisiteScore",
                table: "ChallengeSpecs",
                type: "int",
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
