using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengeGroupsTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "TextSearchVector",
                table: "PracticeChallengeGroups",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Id", "Description" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_TextSearchVector",
                table: "PracticeChallengeGroups",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PracticeChallengeGroups_TextSearchVector",
                table: "PracticeChallengeGroups");

            migrationBuilder.DropColumn(
                name: "TextSearchVector",
                table: "PracticeChallengeGroups");
        }
    }
}
