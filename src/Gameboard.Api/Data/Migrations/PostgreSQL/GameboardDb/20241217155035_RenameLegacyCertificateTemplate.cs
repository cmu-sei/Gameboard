using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class RenameLegacyCertificateTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CertificateTemplate",
                table: "Games",
                newName: "CertificateTemplateLegacy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CertificateTemplateLegacy",
                table: "Games",
                newName: "CertificateTemplate");
        }
    }
}
