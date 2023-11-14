using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Data;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder WithGameboardOptions(this DbContextOptionsBuilder builder, IWebHostEnvironment env)
    {
        // we accommodate for the case that the environment is null (as it is during the creation of migrations)
        // by assuming that we don't need any detailed/sensitive logging - the environment must explicitly be set
        // to activate these behaviors
        if (env is null)
        {
            return builder.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Query.Name }, LogLevel.Debug);
        }

        // warn us about queries that might benefit from query splitting
        builder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

        if (env.IsDevOrTest())
        {
            Console.WriteLine("Starting in the dev environment. Enabling detailed/sensitive EF logging...");
            builder
                .EnableDetailedErrors()
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Query.Name }, LogLevel.Debug);

            if (env.IsDevelopment())
            {
                builder.EnableSensitiveDataLogging();
            }
        }
        else
        {
            builder.LogTo(Console.WriteLine, LogLevel.Warning);
        }

        return builder;
    }
}
