// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Gameboard.Api.Features.ApiKeys;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Gameboard.Api.Structure.Auth;

public static class ApiKeyAuthentication
{
    public const string AuthenticationScheme = "ApiKey";
    public const string ApiKeyHeaderName = "x-api-key";
    public const string AuthorizationHeaderName = "Authorization";
    public const string ChallengeHeaderName = "WWW-Authenticate";
}

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IApiKeysService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IApiKeysService apiKeyService
    ) : base(options, logger, encoder, clock)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var requestApiKey = ResolveRequestApiKey(Request);
        if (requestApiKey.IsEmpty())
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = requestApiKey.ToString();
        var user = await _apiKeyService.Authenticate(apiKey);

        if (user == null)
        {
            Logger.Log(LogLevel.Warning, $"x-api-key authentication failed for key '{apiKey}'.");
            return AuthenticateResult.Fail(new InvalidApiKey(apiKey));
        }

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity
            (
                new Claim[] {
                    new(AppConstants.SubjectClaimName, user.Id),
                    new(AppConstants.NameClaimName, user.Name),
                },
                Scheme.Name
            )
        );

        Logger.Log(LogLevel.Information, $"User {user.ApprovedName} authenticated with x-api-key '{apiKey}'.");
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        return Task.FromResult(AuthenticateResult.Fail(new ApiKeyAuthenticationChallengeException()));
    }

    // previous implementations of this allowed the header in either Authorization or x-api-key, but
    // for now, we're just doing x-api-key to standardize access
    internal string ResolveRequestApiKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue(ApiKeyAuthentication.ApiKeyHeaderName, out StringValues headerApiKey))
            return headerApiKey;

        return null;
    }
}
