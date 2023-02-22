using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Gameboard.Tests.Integration.Fixtures;

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
