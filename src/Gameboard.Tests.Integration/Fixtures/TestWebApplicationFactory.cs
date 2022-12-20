using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration.Fixtures;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _DefaultAuthenticationUserId = "679b1757-8ca7-4816-ad1b-ae90dd1b3941";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            // Add DB context (will ultimately be replaced by a testcontainer)
            services.RemoveService<DbContext>();
            services.AddDbContext<GameboardDbContext>(options => options.UseNpgsql("Username=postgres;Password=testing;Database=Gameboard_db_TEST);"));

            // override authentication/authorization with dummies
            services.Configure<TestAuthenticationHandlerOptions>(options => options.DefaultUserId = _DefaultAuthenticationUserId);
            services
                .AddAuthentication(defaultScheme: TestAuthenticationHandler.AuthenticationSchemeName)
                .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(TestAuthenticationHandler.AuthenticationSchemeName, options => { });
            services.ReplaceService<IAuthorizationService, TestAuthorizationService>();

            // Ensure schema gets created
            var serviceProvider = services.BuildServiceProvider();
            using (var scope = serviceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var context = scopedServices.GetRequiredService<GameboardDbContext>();
                context.Database.EnsureCreated();
            }
        });
    }
}
