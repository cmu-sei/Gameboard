using System.Threading.Tasks;
using Gameboard.Api.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Gameboard.Api.Extensions;

internal static class WebApplicationExtensions
{
    public static WebApplication ConfigureGameboard(this WebApplication app, AppSettings settings)
    {
        app.UseJsonExceptions();

        if (!string.IsNullOrEmpty(settings.PathBase))
            app.UsePathBase(settings.PathBase);

        if (settings.Headers.LogHeaders)
            app.UseHeaderInspection();

        if (!string.IsNullOrEmpty(settings.Headers.Forwarding.TargetHeaders))
            app.UseForwardedHeaders();

        if (settings.Headers.UseHsts)
            app.UseHsts();

        if (app.Environment.IsDev())
            app.UseDeveloperExceptionPage();


        app.UseRouting();
        app.UseCors(settings.Headers.Cors.Name);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFileProtection();
        app.UseStaticFiles();

        if (settings.Logging.EnableHttpLogging)
        {
            app.UseHttpLogging();
        }

        if (settings.OpenApi.Enabled)
            app.UseConfiguredSwagger(settings.OpenApi, settings.Oidc.Audience, settings.PathBase);

        // map endpoints directly on app (warning ASP0014)
        app.MapHub<AppHub>("/hub").RequireAuthorization();
        app.MapControllers().RequireAuthorization();

        return app;
    }
}
