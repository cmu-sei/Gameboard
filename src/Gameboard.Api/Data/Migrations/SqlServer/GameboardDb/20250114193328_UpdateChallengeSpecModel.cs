using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class UpdateChallengeSpecModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertificateHtmlTemplate",
                table: "PracticeModeSettings");

            migrationBuilder.DropColumn(
                name: "TestCode",
                table: "Games");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateHtmlTemplate",
                table: "PracticeModeSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCode",
                table: "Games",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }
    }
}
