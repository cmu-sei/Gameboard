using Gameboard.Api.Data;
using Gameboard.Api.Features.GameEngine;
using Gameboard.Api.Features.Games.External;
using Gameboard.Api.Tests.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Gameboard.Api.Tests.Integration.Fixtures;

[CollectionDefinition(TestCollectionNames.DbFixtureTests)]
public class DbTestCollection : ICollectionFixture<GameboardTestContext> { }

public class GameboardTestContext : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            if (_container is null)
                throw new GbAutomatedTestSetupException("Couldn't initialize the test context - the database contianer hasn't been resolved.");

            // Add DB context with connection to the container
            services.RemoveService<DbContext>();
            // services.AddDbContext<GameboardDbContext, GameboardTestDbContext>(builder => builder.UseNpgsql(_container.GetConnectionString()));
            services.AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>(builder =>
            {
                builder.UseNpgsql(_container.GetConnectionString(), opts => opts.MigrationsAssembly("Gameboard.Api"));
            });

            // Some services (like the stores) in Gameboard inject with GameboardDbContext rather than DbContext,
            // so we need to add an additional binding for them
            services.AddScoped<GameboardDbContextPostgreSQL>();
            services.AddScoped<GameboardDbContext, GameboardDbContextPostgreSQL>();

            // add user claims transformation that lets them all through
            services.ReplaceService<IClaimsTransformation, TestClaimsTransformation>(allowMultipleReplace: true);

            // add a stand-in for external services
            services.ReplaceService<IGamebrainService, TestGamebrainService>();
            services.ReplaceService<IGameEngineService, TestGameEngineService>();

            // dummy authorization service that lets everything through
            services.ReplaceService<IAuthorizationService, TestAuthorizationService>();

            // add defaults for services that can be replaced in .ConfigureTestServices
            services.AddScoped<ITestGradingResultService>(_ => new TestGradingResultService(0, (state) => { }));
        });
    }

    public GameboardDbContext GetDbContext() => Services.GetRequiredService<GameboardDbContext>();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithHostname("localhost")
            .WithPortBinding(5433)
            .WithUsername("foundry")
            .WithPassword("foundry")
            .WithImage("postgres:latest")
            .WithAutoRemove(true)
            .WithCleanUp(true)
            .Build();

        // start up our testcontainer with the db
        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
