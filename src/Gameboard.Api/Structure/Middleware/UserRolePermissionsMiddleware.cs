// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Gameboard.Api.Features.Users;

namespace Gameboard.Api.Structure.Middleware;

public class UserRolePermissionsMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IUserRolePermissionsService userRolePermissionsService)
    {
        var user = await context.User.ToActor(userRolePermissionsService);

        // the "who's the acting user?" service and (old) controllers pull this stuff out to sniff the user's permissions/identity
        context.Items.Add(AppConstants.RequestContextGameboardUser, user);
        context.Items.Add(AppConstants.RequestContextGameboardGraderForChallengeId, context.User.ToAuthenticatedGraderForChallengeId());

        await _next(context);
    }
}

public static class UserRolePermissionsMiddlewareExtensions
{
    public static IApplicationBuilder UseUserRolePermissions(this IApplicationBuilder appBuilder)
        => appBuilder.UseMiddleware<UserRolePermissionsMiddleware>();
}
