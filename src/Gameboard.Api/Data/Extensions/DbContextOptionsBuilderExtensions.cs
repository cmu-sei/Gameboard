using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Data;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder WithGameboardOptions(this DbContextOptionsBuilder builder, IWebHostEnvironment env, ILogger logger)
    {
        if (env.IsDevOrTest())
        {
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
