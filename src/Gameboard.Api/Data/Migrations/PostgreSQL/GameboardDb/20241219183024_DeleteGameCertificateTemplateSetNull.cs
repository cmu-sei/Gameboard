// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gameboard.Api.Data.Migrations.PostgreSQL.GameboardDb
{
    /// <inheritdoc />
    public partial class DeleteGameCertificateTemplateSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId",
                principalTable: "CertificateTemplate",
                principalColumn: "Id");
        }
    }
}
