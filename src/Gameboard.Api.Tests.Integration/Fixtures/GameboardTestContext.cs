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
            services
                .AddDbContext<GameboardDbContext, GameboardDbContextPostgreSQL>(builder =>
                {
                    builder.EnableDetailedErrors();
                    builder.EnableSensitiveDataLogging();
                    builder.UseNpgsql(_container.GetConnectionString());
                }, ServiceLifetime.Transient);

            services
                // add user claims transformation that lets them all through
                .ReplaceService<IClaimsTransformation, TestClaimsTransformation>(allowMultipleReplace: true)

                // add a stand-in for external services
                .ReplaceService<IExternalGameHostService, TestExternalGameHostService>()
                .ReplaceService<IGameEngineService, TestGameEngineService>()

                // dummy authorization service that lets everything through
                .ReplaceService<IAuthorizationService, TestAuthorizationService>()

                // add defaults for services that are sometimes replaced in .ConfigureTestServices
                .AddScoped<ITestGameEngineStateChangeService, TestGameEngineStateChangeService>()
                .AddScoped<ITestGradingResultService>(_ => new TestGradingResultService(new TestGradingResultServiceConfiguration()));
        });
    }

    public GameboardDbContext GetDbContext()
        => Services.GetRequiredService<GameboardDbContext>();

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithHostname("localhost")
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
