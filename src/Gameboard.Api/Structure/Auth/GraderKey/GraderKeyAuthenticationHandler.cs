using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace Gameboard.Api.Structure.Auth;

internal static class GraderKeyAuthentication
{
    public const string AuthenticationScheme = "GraderKey";
    public const string GraderKeyHeaderName = "Grader-Key";
    public const string GraderKeyChallengeIdClaimName = "GraderKeyChallengeId";
}

public class GraderKeyAuthenticationOptions : AuthenticationSchemeOptions { }

internal class GraderKeyUnresolvedChallengeException(string graderKey) : GameboardException($"{nameof(GraderKeyAuthenticationHandler)} - failed authentication attempt - Couldn't resolve a challenge for grader key {graderKey}")
{
}

public static class GraderKeyAuthenticationExtensions
{
    public static AuthenticationBuilder AddGraderKeyAuthentication
    (
        this AuthenticationBuilder builder,
        string scheme,
        Action<GraderKeyAuthenticationOptions> options
    ) => builder.AddScheme<GraderKeyAuthenticationOptions, GraderKeyAuthenticationHandler>
        (
            scheme ?? GraderKeyAuthentication.AuthenticationScheme,
            options
        );
}

internal class GraderKeyAuthenticationHandler : AuthenticationHandler<GraderKeyAuthenticationOptions>
{
    private readonly IStore _store;
    public GraderKeyAuthenticationHandler
    (
        IOptionsMonitor<GraderKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder urlEncoder,
        IStore store
    ) : base(options, loggerFactory, urlEncoder)
    {
        _store = store;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // prefer values set with the Grader-Key header
        if (!Request.Headers.TryGetValue(GraderKeyAuthentication.GraderKeyHeaderName, out StringValues graderKey))
            // but also accept values from the x-api-key header
            if (!Request.Headers.TryGetValue(ApiKeyAuthentication.ApiKeyHeaderName, out graderKey))
                return AuthenticateResult.NoResult();

        var hashedKey = graderKey.ToString().ToSha256();
        var challengeId = await _store
            .WithNoTracking<Data.Challenge>()
            .Where(c => c.GraderKey == hashedKey)
            .Select(c => c.Id)
            .SingleOrDefaultAsync();

        if (challengeId.IsEmpty())
            return AuthenticateResult.Fail(new GraderKeyUnresolvedChallengeException(graderKey));

        var claimsPrincipal = new ClaimsPrincipal
        (
            new ClaimsIdentity
            (
                [new Claim(GraderKeyAuthentication.GraderKeyChallengeIdClaimName, challengeId)],
                Scheme.Name
            )
        );

        Logger.Log(LogLevel.Information, $"Authenticated challenge grader for challenge {challengeId} authenticated with grader key '{graderKey}'.");
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }
}
