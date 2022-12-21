using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Gameboard.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration.Fixtures;

public class GameboardTestContext<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class where TDbContext : DbContext
{
    private readonly string _DefaultAuthenticationUserId = "679b1757-8ca7-4816-ad1b-ae90dd1b3941";
    private readonly TestcontainerDatabase _dbContainer;

    public HttpClient Http { get; }

    public GameboardTestContext()
    {
        _dbContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
            .WithDatabase(new PostgreSqlTestcontainerConfiguration
            {
                Database = "GameboardTestDb",
                Username = "gameboard",
                Password = "gameboard",
            })
            .WithCleanUp(true)
            .Build();

        // start the container (see explanation below in InitializeAsync)
        _dbContainer.StartAsync().Wait();

        // create an HttpClient with the desired defaults
        Http = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Add DB context with connection to the container
            services.RemoveService<TDbContext>();
            services.AddDbContext<TDbContext>(options => options.UseNpgsql(_dbContainer.ConnectionString));

            // override authentication/authorization with dummies
            services.Configure<TestAuthenticationHandlerOptions>(options => options.DefaultUserId = _DefaultAuthenticationUserId);
            services
                .AddAuthentication(defaultScheme: TestAuthenticationHandler.AuthenticationSchemeName)
                .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationSchemeName, options => { });
            services.ReplaceService<IAuthorizationService, TestAuthorizationService>();

            // TODO: figure out why the json options registered in the main app's ConfigureServices aren't here
            // services.AddMvc().AddGameboardJsonOptions();
        });
    }

    public TDbContext GetDbContext()
    {
        return Services.GetRequiredService<TDbContext>();
    }

    public JsonSerializerOptions GetJsonSerializerOptions()
    {
        var defaultOptions = new Microsoft.AspNetCore.Mvc.JsonOptions();
        IMvcBuilderExtensions.BuildJsonOptions()(defaultOptions);

        return defaultOptions.JsonSerializerOptions;
    }

    public async Task InitializeAsync()
    {
        // Would really like to do this here, but this seems to happen after ConfigureWebhost, and I need the
        // connection string before that
        // await _dbContainer.StartAsync();

        // ensure database migration
        await Services.GetService<TDbContext>()!.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
