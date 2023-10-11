using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Hubs;
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

        // We intentionally disable the DeveloperException page by default
        // because it causes API error behavior to differ between dev and compiled environments.
        // Only uncomment this on a temporary basis, and only if you really need the stack trace and 
        // can't use the one provided by your text editor's logging/output tools.
        // if (app.Environment.IsDevelopment())
        //     app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseCors(settings.Headers.Cors.Name);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseFileProtection();
        app.UseStaticFiles();

        if (settings.Logging.EnableHttpLogging)
            app.UseHttpLogging();

        if (settings.OpenApi.Enabled)
            app.UseConfiguredSwagger(settings.OpenApi, settings.Oidc.Audience, settings.PathBase);

        // map endpoints directly on app (warning ASP0014)
        app.MapHub<AppHub>("/hub").RequireAuthorization();
        app.MapHub<GameHub>("/hub/games").RequireAuthorization();
        app.MapHub<ScoreHub>("/hub/scores").RequireAuthorization();
        app.MapControllers().RequireAuthorization();

        return app;
    }
}
