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
            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Sponsor",
                table: "Players");

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

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "Players");

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
        }
    }
}
