using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddGameAndChallengeSpecSearchVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "TextSearchVector",
                table: "Games",
                type: "tsvector",
                nullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Competition", "Id", "Track", "Season", "Division" });

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TextSearchVector",
                table: "ChallengeSpecs",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "GameId", "Tag", "Tags", "Text" })
                .OldAnnotation("Npgsql:TsVectorConfig", "english")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "Tag", "Tags" });

            migrationBuilder.CreateIndex(
                name: "IX_Games_TextSearchVector",
                table: "Games",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_TextSearchVector",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "TextSearchVector",
                table: "Games");

            migrationBuilder.AlterColumn<NpgsqlTsVector>(
                name: "TextSearchVector",
                table: "ChallengeSpecs",
                type: "tsvector",
                nullable: true,
                oldClrType: typeof(NpgsqlTsVector),
                oldType: "tsvector",
                oldNullable: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "Tag", "Tags" })
                .OldAnnotation("Npgsql:TsVectorConfig", "english")
                .OldAnnotation("Npgsql:TsVectorProperties", new[] { "Name", "Description", "GameId", "Tag", "Tags", "Text" });
        }
    }
}
