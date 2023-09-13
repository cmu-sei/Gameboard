using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddSponsorFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // add new columns
            migrationBuilder.AddColumn<bool>(
                name: "HasDefaultSponsor",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SponsorId",
                table: "Users",
                type: "nvarchar(40)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SponsorId",
                table: "Players",
                type: "nvarchar(40)",
                nullable: false,
                defaultValue: "");

            // HasDefaultSponsor is true if the user has a blank or null sponsor before the migration
            // (and false otherwise)
            migrationBuilder.Sql
            ("""
                UPDATE Users
                SET HasDefaultSponsor = ISNULL(Sponsor, '') = '';
            """);

            // migrate data from old columns
            migrationBuilder.Sql
            ("""
                UPDATE u SET u.SponsorId = s.Id
                FROM Users u 
                INNER JOIN Sponsors s ON s.Id = u.SponsorId
                WHERE s.Logo = u.Sponsor
                    AND ISNULL(u.Sponsor, '') != '';
                
                UPDATE Users SET SponsorId = 'other' WHERE ISNULL(Sponsor, '') = '';
            """);

            migrationBuilder.Sql
            ("""
                UPDATE p SET p.SponsorId = s.Id
                FROM Players p 
                INNER JOIN Sponsors s ON s.Id = p.SponsorId
                WHERE s.Logo = p.Sponsor
                    AND ISNULL(p.Sponsor, '') != '';

                UPDATE Players SET SponsorId = 'other' WHERE ISNULL(Sponsor, '') = '';
            """);

            // create indices/FKs
            migrationBuilder.CreateIndex(
                name: "IX_Users_SponsorId",
                table: "Users",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_SponsorId",
                table: "Players",
                column: "SponsorId");

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
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sponsor",
                table: "Players",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            // migrate data from new columns to old
            migrationBuilder.Sql
            ("""
                UPDATE u
                SET u.Sponsor = s.Logo
                FROM Users u
                INNER JOIN Sponsors s ON s.Id = u.SponsorId;
            """);

            migrationBuilder.Sql
            ("""
                UPDATE p
                SET p.Sponsor = s.Logo
                FROM Players p 
                INNER JOIN Sponsors s ON s.Id = p.SponsorId;
            """);

            // drop new columns
            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Players");
        }
    }
}
