using Gameboard.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Tests.Integration.Fixtures;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly string _DefaultAuthenticationUserId = "679b1757-8ca7-4816-ad1b-ae90dd1b3941";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("test");
        builder.ConfigureServices(services =>
        {
            // remove configured db context
            // Remove AppDbContext
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GameboardDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Add DB context pointing to test container
            services.AddDbContext<GameboardDbContext>(options => options.UseNpgsql("Username=postgres;Password=testing;Database=Gameboard_db_TEST);"));

            // configure authorization spoof
            // services.AddAuthorization(_ =>
            // {
            //     _.AddPolicy("AutomatedTest", new AuthorizationPolicyBuilder()
            //         .RequireAssertion(context => context.Succeed())
            //     );
            // });

            services.Configure<TestAuthenticationHandlerOptions>(options => options.DefaultUserId = _DefaultAuthenticationUserId);

            services.AddAuthentication(TestAuthenticationHandlerOptions.AuthenticationSchemeName)
                .AddScheme<TestAuthenticationHandlerOptions, TestAuthenticationHandler>(TestAuthenticationHandlerOptions.AuthenticationSchemeName, options => { });

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
