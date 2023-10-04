using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddParentSponsor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentSponsorId",
                table: "Sponsors",
                type: "nvarchar(40)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_ParentSponsorId",
                table: "Sponsors",
                column: "ParentSponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sponsors_Sponsors_ParentSponsorId",
                table: "Sponsors",
                column: "ParentSponsorId",
                principalTable: "Sponsors",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sponsors_Sponsors_ParentSponsorId",
                table: "Sponsors");

            migrationBuilder.DropIndex(
                name: "IX_Sponsors_ParentSponsorId",
                table: "Sponsors");

            migrationBuilder.DropColumn(
                name: "ParentSponsorId",
                table: "Sponsors");
        }
    }
}
