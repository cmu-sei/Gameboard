using System;
using Gameboard.Api.Services;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Gameboard.Api.Data.Migrations;

public static class MigrationExtensions
{
    public static OperationBuilder<InsertDataOperation> InsertReport(this MigrationBuilder builder, Report report)
    {
        if (report.Id.NotEmpty())
            throw new GameboardException("Don't pass an ID when inserting a report - we do that for you 👍");

        return builder.InsertData
        (
            "Reports",
            new string[]
            {
                nameof(report.Id),
                nameof(report.Key),
                nameof(report.Name),
                nameof(report.Description),
                nameof(report.ExampleFields),
                nameof(report.ExampleParameters)
            },
            new string[]
            {
                GuidService.StaticGenerateGuid(),
                report.Key,
                report.Name,
                report.Description,
                report.ExampleFields,
                report.ExampleParameters
            }
        );
    }
}
