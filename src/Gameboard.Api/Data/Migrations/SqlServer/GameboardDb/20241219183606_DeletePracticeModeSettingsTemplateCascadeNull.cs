using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class DeletePracticeModeSettingsTemplateCascadeNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
                table: "PracticeModeSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
                table: "PracticeModeSettings");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");
        }
    }
}
