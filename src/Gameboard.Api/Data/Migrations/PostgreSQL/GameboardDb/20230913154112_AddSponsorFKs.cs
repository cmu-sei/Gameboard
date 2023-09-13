using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddSponsorFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns
            migrationBuilder.AddColumn<bool>(
                name: "HasDefaultSponsor",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SponsorId",
                table: "Users",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "other");

            migrationBuilder.AddColumn<string>(
                name: "SponsorId",
                table: "Players",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "other");

            // migrate data from old columns
            migrationBuilder.Sql
            ("""
                UPDATE "Users" SET "SponsorId" = "Sponsors"."Id"
                FROM "Sponsors" 
                WHERE "Sponsors"."Logo" = "Users"."Sponsor"
                    AND "Users"."Sponsor" IS NOT NULL;

                UPDATE "Users" SET "SponsorId" = (SELECT "Id" FROM "Sponsors" WHERE "Id" = 'other')
                WHERE "SponsorId" IS NULL;
            """);

            migrationBuilder.Sql
            ("""
                UPDATE "Players" SET "SponsorId" = "Sponsors"."Id"
                FROM "Sponsors" 
                WHERE "Sponsors"."Logo" = "Players"."Sponsor"
                    AND "Players"."Sponsor" IS NOT NULL;;

                UPDATE "Players" SET "SponsorId" = (SELECT "Id" FROM "Sponsors" WHERE "Id" = 'other')
                WHERE "SponsorId" IS NULL;
            """);

            // create indices
            migrationBuilder.CreateIndex(
                name: "IX_Users_SponsorId",
                table: "Users",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_SponsorId",
                table: "Players",
                column: "SponsorId");

            // add fks
            migrationBuilder.AddForeignKey(
                name: "FK_Players_Sponsors_SponsorId",
                table: "Players",
                column: "SponsorId",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Sponsors_SponsorId",
                table: "Users",
                column: "SponsorId",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // drop old columns
            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Players");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_Sponsors_SponsorId",
                table: "Players");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Sponsors_SponsorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_SponsorId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Players_SponsorId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "HasDefaultSponsor",
                table: "Users");

            // resurrect old columns
            migrationBuilder.AddColumn<string>(
                name: "Sponsor",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sponsor",
                table: "Players",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            // migrate data from new columns to old
            migrationBuilder.Sql
            ("""
                UPDATE "Users" SET "Sponsor" = "Sponsors"."Logo"
                FROM "Sponsors" 
                WHERE "Sponsors"."Id" = "Users"."SponsorId";
            """);

            // drop new columns
            migrationBuilder.Sql
            ("""
                UPDATE "Players" SET "Sponsor" = "Sponsors"."Logo"
                FROM "Sponsors" 
                WHERE "Sponsors"."Id" = "Players"."SponsorId";
            """);

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Players");
        }
    }
}
