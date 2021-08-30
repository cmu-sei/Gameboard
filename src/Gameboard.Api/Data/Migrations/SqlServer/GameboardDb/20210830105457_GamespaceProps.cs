using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    public partial class GamespaceProps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RegistrationMarkdown",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationConstraint",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardText1",
                table: "Games",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardText2",
                table: "Games",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardText3",
                table: "Games",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameMarkdown",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Games",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Games",
                type: "nvarchar(40)",
                maxLength: 40,
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

            migrationBuilder.DropColumn(
                name: "GameMarkdown",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Games");

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationMarkdown",
                table: "Games",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RegistrationConstraint",
                table: "Games",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
