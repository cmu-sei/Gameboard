using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class UpdateUserAndPlayerPendingNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Users" SET "NameStatus" = '' WHERE "NameStatus" = 'pending' AND "ApprovedName" = "Name";""");
            migrationBuilder.Sql("""UPDATE "Players" SET "NameStatus" = '' WHERE "NameStatus" = 'pending' AND "ApprovedName" = "Name";""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
