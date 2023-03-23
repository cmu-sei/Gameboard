using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class RemoveApiKeyOwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyOwnerId",
                table: "Users");

            migrationBuilder.Sql("TRUNCATE TABLE \"ApiKeys\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyOwnerId",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }
    }
}
