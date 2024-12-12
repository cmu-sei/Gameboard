using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class AddFeedbackWhenFinalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "FeedbackSubmissions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WhenFinalized",
                table: "FeedbackSubmissions",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhenFinalized",
                table: "FeedbackSubmissions");

            migrationBuilder.AddColumn<string>(
                name: "TeamId",
                table: "FeedbackSubmissions",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
