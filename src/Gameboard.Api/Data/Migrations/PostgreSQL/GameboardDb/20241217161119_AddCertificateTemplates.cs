using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddCertificateTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertificateTemplateId",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PracticeCertificateTemplateId",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CertificateTemplate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateTemplate_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplate_CreatedByUserId",
                table: "CertificateTemplate",
                column: "CreatedByUserId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.DropTable(
                name: "CertificateTemplate");

            migrationBuilder.DropIndex(
                name: "IX_Games_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PracticeCertificateTemplateId",
                table: "Games");
        }
    }
}
