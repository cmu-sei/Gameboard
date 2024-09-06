﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.SqlServer.GameboardDb
{
    /// <inheritdoc />
    public partial class MigrateUserRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql
            ("""
                DO $$
                DECLARE unmigratedUsers INTEGER;
                BEGIN
                    SELECT INTO unmigratedUsers COUNT(*) FROM "Users" WHERE "Role" > 4;

                    IF unmigratedUsers > 0 THEN
                        WITH upd AS(
                            UPDATE "Users" SET "Role" = CASE
                                WHEN "Role" = 0 THEN 0
                                WHEN CAST("Role" AS BIT(7)) & B'0100000' = b'0100000' THEN 4
                                WHEN CAST("Role" AS BIT(7)) & B'0010000' = B'0010000' THEN 3
                                WHEN CAST("Role" AS BIT(7)) & B'1000000' = B'1000000' THEN 2
                                WHEN "Role" != 0 THEN 1
                                ELSE 0
                            END
                            WHERE "Role" != 0
                        )
                        SELECT * FROM "Update";
                    END IF;
                END; $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}