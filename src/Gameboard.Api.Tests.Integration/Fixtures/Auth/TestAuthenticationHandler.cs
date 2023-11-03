using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationHandlerOptions>
{
    private readonly TestAuthenticationUser _actingUser;

    public const string AuthenticationSchemeName = "Test";
    public AuthenticationScheme AuthScheme { get; } = new AuthenticationScheme
    (
        name: AuthenticationSchemeName,
        displayName: AuthenticationSchemeName,
        handlerType: typeof(TestAuthenticationHandler)
    );

    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) :
        base(options, logger, encoder, clock)
    {
        _actingUser = options.CurrentValue.Actor;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _actingUser.Name),
            new(ClaimTypes.NameIdentifier, _actingUser.Id),
            new(AppConstants.SubjectClaimName, _actingUser.Id),
            new(AppConstants.ApprovedNameClaimName, _actingUser.Name),
            new(AppConstants.RoleListClaimName, _actingUser.Role.ToString()),
            new(AppConstants.SponsorClaimName, _actingUser.SponsorId)
        };

        var identity = new ClaimsIdentity(claims, AuthScheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthScheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
