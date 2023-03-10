using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration.Fixtures;

public class GameboardTestContext<TDbContext> : WebApplicationFactory<Program>, IAsyncLifetime where TDbContext : GameboardDbContext
{
    private readonly TestcontainerDatabase _dbContainer;

    public GameboardTestContext()
    {
        _dbContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "GameboardIntegrationTestDb",
                Username = "gameboard",
                Password = "gameboard",
            })
            .WithImage("postgres:latest")
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Add DB context with connection to the container
            services.RemoveService<TDbContext>();
            services.AddDbContext<TDbContext>(options => options.UseNpgsql(_dbContainer.ConnectionString));

            // Some services (like the stores) in Gameboard inject with GameboardDbContext rather than DbContext,
            // so we need to add an additional binding for them
            services.AddTransient<GameboardDbContext, TDbContext>();

            // add user claims transformation that lets them all through
            services.ReplaceService<IClaimsTransformation, TestClaimsTransformation>(allowMultipleReplace: true);

            // add a stand-in for the game engine service for now, because we don't have an instance for integration tests 
            services.ReplaceService<IGameEngineService, TestGameEngineService>();

            // dummy authorization service that lets everything through
            services.ReplaceService<IAuthorizationService, TestAuthorizationService>();
        });
    }

    public TDbContext GetDbContext()
    {
        return Services.GetRequiredService<TDbContext>();
    }

    public async Task InitializeAsync()
    {
        // start up our testcontainer with the db
        await _dbContainer.StartAsync();

        // get the dbcontext type and use it to migrate (stand up) the database
        var dbContext = Services.GetRequiredService<TDbContext>();
        if (dbContext == null)
        {
            throw new MissingServiceException<TDbContext>("Attempting to stand up the testcontainers database but hit a missing dbContext service.");
        }

        // ensure database migration
        await Services.GetService<TDbContext>()!.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
