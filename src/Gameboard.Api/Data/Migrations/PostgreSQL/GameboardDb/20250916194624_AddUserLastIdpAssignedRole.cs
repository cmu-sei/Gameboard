using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class AddUserLastIdpAssignedRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastIdpAssignedRole",
                table: "Users",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastIdpAssignedRole",
                table: "Users");
        }
    }
}
