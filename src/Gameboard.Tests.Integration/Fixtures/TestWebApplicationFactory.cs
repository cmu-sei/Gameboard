using System.Data.Common;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Tests.Integration.Fixtures;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("test");
        builder.ConfigureServices(services =>
        {
            // remove configured db context
            var dbContextDescriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<GameboardDbContext>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            // remove configured connection string
            var dbConnectionString = services.SingleOrDefault(s => s.ServiceType == typeof(DbConnection));
            if (dbConnectionString != null)
                services.Remove(dbConnectionString);

            // create in-memory db provider/connection
            services.AddSingleton<DbConnection>(container =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                return connection;
            });

            services.AddDbContext<GameboardDbContext>((container, options) =>
            {
                var connection = container.GetRequiredService<DbConnection>();
                options.UseSqlite(connection);
            });
        });
    }
}
