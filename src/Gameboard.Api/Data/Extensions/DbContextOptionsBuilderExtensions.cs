using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder WithGameboardOptions(this DbContextOptionsBuilder builder, IWebHostEnvironment env)
    {
        if (env.IsDev())
        {
            Console.WriteLine("Starting in the dev environment. Enabling detailed/sensitive EF logging...");
            builder
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Query.Name });
        }

        return builder;
    }
}
