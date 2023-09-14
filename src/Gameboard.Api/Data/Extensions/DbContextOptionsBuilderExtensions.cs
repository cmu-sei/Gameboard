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
        logger.LogInformation($"""Configuring EF Logging for environment "{env.EnvironmentName}".""");
        if (env.IsDevOrTest())
        {
            logger.LogInformation($""" In non-production-configuration "{env.EnvironmentName}". Configuring verbose logging.""");
            builder
                .EnableDetailedErrors()
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Query.Name }, LogLevel.Debug);

            if (env.IsDevelopment())
            {
                logger.LogInformation($"""In Development env ("{env.EnvironmentName}"). Enabling sensitive data logging.""");
                builder.EnableSensitiveDataLogging();
            }
        }
        else
        {
            logger.LogInformation($"""In production environment ("{env.EnvironmentName}"). Configuring minimal logging.""");
            builder.LogTo(Console.WriteLine, LogLevel.Warning);
        }

        return builder;
    }
}
