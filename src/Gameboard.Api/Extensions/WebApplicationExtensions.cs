// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Games;
using Gameboard.Api.Features.Hubs;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Hubs;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.Middleware;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

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

        // additional post-build logging config
        app.UseSerilogRequestLogging(opt =>
        {
            opt.IncludeQueryInRequestPath = true;
            opt.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("IsApiRequest", true);
                diagnosticContext.Set("UserIPv4", httpContext.Connection.RemoteIpAddress);

                var user = httpContext.Items[AppConstants.RequestContextGameboardUser] as User;
                if (user is not null)
                {
                    diagnosticContext.Set("UserId", user.Id);
                    diagnosticContext.Set("UserRole", user.Role);
                    diagnosticContext.Set("UserName", user.ApprovedName);
                }
            };
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseUserRolePermissions();
        app.UseFileProtection();
        app.UseStaticFiles();

        if (settings.OpenApi.Enabled)
            app.UseConfiguredSwagger(settings.OpenApi, settings.Oidc.Audience, settings.PathBase);

        // map endpoints directly on app (warning ASP0014)
        app.MapHub<AppHub>("/hub").RequireAuthorization();
        app.MapHub<GameHub>("/hub/games").RequireAuthorization();
        app.MapHub<ScoreHub>("/hub/scores").RequireAuthorization();
        app.MapHub<SupportHub>("/hub/support").RequireAuthorization();
        app.MapHub<UserHub>("/hub/users").RequireAuthorization();
        app.MapControllers().RequireAuthorization();

        return app;
    }

    /// <summary>
    /// This is where we put things that we want to happen when Gameboard boots, but before the API goes live.
    /// Currently, this only involves syncing challenge spec data for "active" challenge specs. 
    /// 
    /// Be very careful about adding additional work to this function. It's good to be able to do things on startup
    /// sometimes, but we don't want app reboot to become a lengthy process.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    public static async Task<WebApplication> DoStartupTasks(this WebApplication app, Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            using var serviceScope = app.Services.CreateScope();
            var taskQueue = serviceScope.ServiceProvider.GetRequiredService<IBackgroundAsyncTaskQueueService>();

            await taskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
            {
                using var queueScope = app.Services.CreateScope();
                var challengeSpecService = queueScope.ServiceProvider.GetRequiredService<ChallengeSpecService>();
                await challengeSpecService.SyncActiveSpecs(CancellationToken.None);
                logger.LogInformation("Synchronized active challenge specs on startup.");

                var mediator = queueScope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Publish(new AppStartupNotification(), cancellationToken);
                logger.LogInformation("All startup tasks complete.");
            });
        }
        catch (Exception ex)
        {
            logger.LogError(message: "Failed on startup tasks:", exception: ex);
        }

        return app;
    }
}
