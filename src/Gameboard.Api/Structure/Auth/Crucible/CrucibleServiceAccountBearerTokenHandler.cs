// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Gameboard.Api.Structure.Auth.Crucible;

public class CrucibleServiceAccountBearerTokenHandler
(
    ILogger<CrucibleServiceAccountBearerTokenHandler> logger,
    ICrucibleServiceAccountTokenService tokenService
) : DelegatingHandler
{
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.Unauthorized)
        .WaitAndRetryForeverAsync(attempt =>
        {
            logger.LogError("Retrying crucible service account authorization after a 401");
            tokenService.InvalidateToken();
            return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 120));
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(async () =>
        {
            var token = await tokenService.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);
            return await base.SendAsync(request, cancellationToken);
        });

        // result.Result is null/default if the policy fails
        return result.Result ?? result.FinalHandledResult;
    }
}
