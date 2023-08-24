using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Gameboard.Api.Auth;
using Gameboard.Api.Data;
using Gameboard.Api.Data.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Gameboard.Api.Structure.Auth;

internal static class GraderKeyAuthentication
{
    public const string AuthenticationScheme = "GraderKey";
    public const string GraderKeyHeaderName = "Grader-Key";
    public const string GraderKeyChallengeIdClaimName = "GraderKeyChallengeId";
}

public class GraderKeyAuthenticationOptions : AuthenticationSchemeOptions { }

internal class GraderKeyUnresolvedChallengeException : GameboardException
{
    public GraderKeyUnresolvedChallengeException(string graderKey) : base($"{nameof(GraderKeyAuthenticationHandler)} - failed authentication attempt - Couldn't resolve a challenge for grader key {graderKey}") { }
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
    private readonly IChallengeStore _challengeStore;
    public GraderKeyAuthenticationHandler
    (
        IOptionsMonitor<GraderKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder urlEncoder,
        ISystemClock sysClock,
        IChallengeStore challengeStore
    ) : base(options, loggerFactory, urlEncoder, sysClock)
    {
        _challengeStore = challengeStore;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // prefer values set with the Grader-Key header
        if (!Request.Headers.TryGetValue(GraderKeyAuthentication.GraderKeyHeaderName, out StringValues graderKey))
            // but also accept values from the x-api-key header
            if (!Request.Headers.TryGetValue(ApiKeyAuthentication.ApiKeyHeaderName, out graderKey))
                return AuthenticateResult.NoResult();


        var hashedKey = graderKey.ToString().ToSha256();
        var challenge = await _challengeStore
            .List()
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.GraderKey == hashedKey);

        if (challenge is null)
            return AuthenticateResult.Fail(new GraderKeyUnresolvedChallengeException(graderKey));

        var claimsPrincipal = new ClaimsPrincipal
        (
            new ClaimsIdentity
            (
                new Claim[] { new Claim(GraderKeyAuthentication.GraderKeyChallengeIdClaimName, challenge.Id) },
                Scheme.Name
            )
        );

        Logger.Log(LogLevel.Information, $"Authenticated challenge grader for challenge {challenge.Id} authenticated with grader key '{graderKey}'.");
        return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name));
    }
}
