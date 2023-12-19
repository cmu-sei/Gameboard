using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
        // We usually don't mind this, because split queries only matter if tables are very large, but 
        // uncomment this to find issues if performance suffers or if warnings about split querying are 
        // shown
        // builder.ConfigureWarnings(w => w.Throw(RelationalEventId.MultipleCollectionIncludeWarning));

        if (env.IsDevOrTest())
        {
            builder.LogTo(Console.WriteLine, new[] { DbLoggerCategory.Query.Name }, LogLevel.Information);

            if (env.IsDevelopment())
            {
                Console.WriteLine("Starting in dev environment. Enabling detailed/sensitive EF logging...");
                builder
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging();
            }
        }
        else
        {
            builder.LogTo(Console.WriteLine, LogLevel.Warning);
        }

        return builder;
    }
}
