using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddAndReviseExternalGameEndpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ExternalGameStartupUrl",
                table: "Games",
                newName: "ExternalGameStartupEndpoint");

            migrationBuilder.AddColumn<string>(
                name: "ExternalGameTeamExtendedEndpoint",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalGameTeamExtendedEndpoint",
                table: "Games");

            migrationBuilder.RenameColumn(
                name: "ExternalGameStartupEndpoint",
                table: "Games",
                newName: "ExternalGameStartupUrl");
        }
    }
}
