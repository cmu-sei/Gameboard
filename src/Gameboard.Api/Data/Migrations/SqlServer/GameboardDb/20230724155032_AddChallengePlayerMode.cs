using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddChallengePlayerMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerMode",
                table: "Challenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlayerMode",
                table: "ArchivedChallenges",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(""" 
                UPDATE c
                    SET c.PlayerMode = g.PlayerMode
                FROM Challenges c 
                INNER JOIN Games g ON g.Id = c.GameId;
            """);

            migrationBuilder.Sql(""" 
                UPDATE ac
                    SET c.PlayerMode = g.PlayerMode
                FROM ArchivedChallenges ac 
                INNER JOIN Games g ON g.Id = ac.GameId;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayerMode",
                table: "Challenges");

            migrationBuilder.DropColumn(
                name: "PlayerMode",
                table: "ArchivedChallenges");
        }
    }
}
