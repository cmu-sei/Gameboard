using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddApiKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyOwnerId",
                table: "Users",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true,
                defaultValueSql: "REPLACE(NEWID(), '-', '')");

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GeneratedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETDATE()"),
                    ExpiresOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true, defaultValueSql: "NULL"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnerId = table.Column<string>(type: "nvarchar(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerId",
                table: "ApiKeys",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "ApiKeyOwnerId",
                table: "Users");
        }
    }
}
