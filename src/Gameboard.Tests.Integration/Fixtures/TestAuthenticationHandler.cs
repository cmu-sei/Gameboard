using System.Security.Claims;
using System.Text.Encodings.Web;
using Gameboard.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gameboard.Tests.Integration.Fixtures;

internal class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationHandlerOptions>
{
    private readonly string _defaultUserId;

    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) :
        base(options, logger, encoder, clock)
    {
        _defaultUserId = options.CurrentValue.DefaultUserId;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Test user") };

        // Extract User ID from the request headers if it exists,
        // otherwise use the default User ID from the options.
        if (Context.Request.Headers.TryGetValue("UserId", out var userIds))
        {
            if (userIds.Count() == 1 && userIds[0] != null && userIds[0] != string.Empty)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userIds[0]!));
            }
            else
            {
                throw new CantResolveAuthUserIdException(userIds);
            }
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, _defaultUserId));
            claims.Add(new Claim(AppConstants.SubjectClaimName, _defaultUserId));
        }

        // TODO: Add as many claims as you need here
        var identity = new ClaimsIdentity(claims, Options.AuthScheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Options.AuthScheme.Name);

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}
