using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class AddGameKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Games",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Key",
                table: "Games");
        }
    }
}
