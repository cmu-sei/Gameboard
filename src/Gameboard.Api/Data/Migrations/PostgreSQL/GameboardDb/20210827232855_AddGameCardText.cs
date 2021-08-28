using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class AddGameCardText : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardText1",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardText2",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardText3",
                table: "Games",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardText1",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CardText2",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "CardText3",
                table: "Games");
        }
    }
}
