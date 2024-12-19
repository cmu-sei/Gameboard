using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddPracticeModeDefaultTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateTemplateId",
                table: "PracticeModeSettings",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeModeSettings_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplat~",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplat~",
                table: "PracticeModeSettings");

            migrationBuilder.DropIndex(
                name: "IX_PracticeModeSettings_CertificateTemplateId",
                table: "PracticeModeSettings");

            migrationBuilder.DropColumn(
                name: "CertificateTemplateId",
                table: "PracticeModeSettings");
        }
    }
}
