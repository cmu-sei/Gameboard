using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class MigrateCompetitiveCertificateTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql
            ("""
                DO $$
                DECLARE AdminUserId varchar(50) := '';
                DECLARE GameRow record;
                DECLARE Template TEXT := '';
                BEGIN
                    AdminUserId:= (SELECT "Id" FROM "Users" WHERE "Role" = 4 LIMIT 1);
                    IF AdminUserId IS NULL THEN
                        RETURN;
                    END IF;
                    
                    FOR GameRow IN 
                        SELECT "Id", "Name", "CertificateTemplateLegacy" FROM "Games" WHERE COALESCE("CertificateTemplateLegacy", '') != '' ORDER BY 1
                    LOOP
                        IF EXISTS(SELECT * FROM "CertificateTemplate" WHERE "Name" = GameRow."Name" || ' Template') THEN
                            CONTINUE;
                        END IF;
                        
                        Template = (GameRow."CertificateTemplateLegacy");

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
                            GameRow."Name" || ' Template',
                            Template,
                            AdminUserId
                        );

                        UPDATE "Games" SET "CertificateTemplateId" = (SELECT "Id" FROM "CertificateTemplate" WHERE "Name" = GameRow."Name" || ' Template')
                        WHERE "Id" = GameRow."Id";
                    END LOOP;
                END;
                $$
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
