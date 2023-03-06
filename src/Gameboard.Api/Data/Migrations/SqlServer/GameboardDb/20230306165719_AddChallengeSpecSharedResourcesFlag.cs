using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengeSpecSharedResourcesFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Mode",
                table: "Players",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerMode",
                table: "Games",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GameEngineType",
                table: "ChallengeSpecs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseSharedResources",
                table: "ChallengeSpecs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GameEngineType",
                table: "Challenges",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerMode",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GameEngineType",
                table: "ChallengeSpecs");

            migrationBuilder.DropColumn(
                name: "UseSharedResources",
                table: "ChallengeSpecs");

            migrationBuilder.DropColumn(
                name: "GameEngineType",
                table: "Challenges");
        }
    }
}
