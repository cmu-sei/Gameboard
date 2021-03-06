// Copyright 2020 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Data.SqlServer.Migrations
{
    public partial class AddSlugSharedIdProblem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SharedId",
                table: "Problems",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Problems",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SharedId",
                table: "Problems");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Problems");
        }
    }
}

