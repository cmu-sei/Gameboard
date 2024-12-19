using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
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
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeModeSettings_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                unique: true,
                filter: "[CertificateTemplateId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");

            migrationBuilder.Sql
            ("""
                DO $$
                    DECLARE AdminUserId varchar(50) := '';
                    DECLARE Template TEXT := '';
                    BEGIN
                        IF EXISTS(SELECT * FROM "PracticeModeSettings" WHERE "CertificateHtmlTemplate" IS NOT NULL)  THEN
                            Template:= (SELECT "CertificateHtmlTemplate" from "PracticeModeSettings");
                        END IF;

                        AdminUserId:= (SELECT "Id" FROM "Users" WHERE "Role" = 4 LIMIT 1);
                        IF AdminUserId IS NULL THEN
                            RETURN;
                        END IF;

                        IF EXISTS(SELECT * FROM "CertificateTemplate" WHERE "Name" = 'Practice Area Default Template') THEN
                            RETURN;
                        END IF;

                        INSERT INTO "CertificateTemplate"
                        (
                            "Id",
                            "Name",
                            "Content",
                            "CreatedByUserId"
                        )
                        VALUES
                        (
                            gen_random_uuid(),
                            'Practice Area Default Template',
                            Template,
                            AdminUserId
                        );

                        UPDATE "PracticeModeSettings" SET "CertificateTemplateId" = (SELECT "Id" FROM "CertificateTemplate" WHERE "Name" = 'Practice Area Default Template');
                    END;
                $$
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplateId",
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
