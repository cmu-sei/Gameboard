// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthorizationService : IAuthorizationService
{
    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        return await Task.FromResult(AuthorizationResult.Success());
    }

    public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return await Task.FromResult(AuthorizationResult.Success());
    }
}
