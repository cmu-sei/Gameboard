﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    public partial class NameStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NameStatus",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameStatus",
                table: "Players",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NameStatus",
                table: "Players");
        }
    }
}
