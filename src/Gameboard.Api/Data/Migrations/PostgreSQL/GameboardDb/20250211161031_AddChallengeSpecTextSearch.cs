using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengeSpecTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "TextSearchVector",
                table: "ChallengeSpecs",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "Tag", "Tags" });

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSpecs_TextSearchVector",
                table: "ChallengeSpecs",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChallengeSpecs_TextSearchVector",
                table: "ChallengeSpecs");

            migrationBuilder.DropColumn(
                name: "TextSearchVector",
                table: "ChallengeSpecs");
        }
    }
}
