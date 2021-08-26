using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    public partial class AddGameKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Games",
                type: "nvarchar(max)",
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
