// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<TestAuthenticationHandlerOptions>(options, logger, encoder)
{
    private readonly TestAuthenticationUser _actingUser = options.CurrentValue.Actor;

    public const string AuthenticationSchemeName = "Test";
    public AuthenticationScheme AuthScheme { get; } = new AuthenticationScheme
    (
        name: AuthenticationSchemeName,
        displayName: AuthenticationSchemeName,
        handlerType: typeof(TestAuthenticationHandler)
    );

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _actingUser.Name),
            new(ClaimTypes.NameIdentifier, _actingUser.Id),
            new(AppConstants.SubjectClaimName, _actingUser.Id),
            new(AppConstants.ApprovedNameClaimName, _actingUser.Name),
            new(AppConstants.RoleClaimName, _actingUser.Role.ToString()),
            new(AppConstants.SponsorClaimName, _actingUser.SponsorId)
        };

        var identity = new ClaimsIdentity(claims, AuthScheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthScheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
