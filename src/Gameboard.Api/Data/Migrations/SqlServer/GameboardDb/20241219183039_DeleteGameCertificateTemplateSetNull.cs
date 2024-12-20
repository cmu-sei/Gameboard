using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class DeleteGameCertificateTemplateSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");
        }
    }
}
