using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class Feedback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Answers",
                table: "Feedback",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(8192)",
                oldMaxLength: 8192,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Answers",
                table: "Feedback",
                type: "character varying(8192)",
                maxLength: 8192,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
